# Piece×Peace — Script・アーキテクチャドキュメント

> 使用言語: C# / Unity  
> 制作期間: 約9ヶ月  
> 制作時期: 2025/04〜2026/01  
> 制作形態: チーム制作（6名）  
> 依頼元: 株式会社 Game for It

---

## 目次

1. [Scriptコード抜粋](#1-scriptコード抜粋)
2. [システム全体像](#2-システム全体像)
3. [アーキテクチャ詳細](#3-アーキテクチャ詳細)
   - [ディレクトリ構成](#31-ディレクトリ構成)
   - [Framework層設計](#32-framework層設計)
   - [Manager層設計](#33-manager層設計)
   - [UI層設計](#34-ui層設計)
   - [GameLogic層（Archive）](#35-gamelogic層archive)
   - [外部パッケージ依存](#36-外部パッケージ依存)
   - [GoFデザインパターン対応表（23パターン）](#37-gofデザインパターン対応表23パターン)
   - [Addressables構成](#38-addressables構成)
   - [既知の問題・要改善点](#39-既知の問題要改善点)

---

## 1. Scriptコード抜粋

### ① 汎用ObjectPool — `ObjectPool.cs` / `APooledObject.cs`

使う側は `Get() / Release()` の2行のみ。二度戻し防止・ライフサイクル通知・上限管理を基底クラスで一括担保。

```csharp
// 使う側：Shooter.cs
pool = new ObjectPool<Piece>(pieceFactory, initial: 3, max: 12);
pool.Prewarm();           // 事前生成して非アクティブで積む
var b = pool.Get();       // 取り出す
// Bullet側で当たり判定後に
b.Release();              // 返す（二度戻し防止済み）

// 基底クラス：APooledObject.cs
public void Release() {
    if (_inPool) return;  // 二度戻し防止
    _inPool = true;
    OnDespawn();
    _return?.Invoke(this);
}

// 派生クラスはオーバーライドするだけ
public class piece : APooledObject {
    protected override void OnSpawn()   { /* 初期化処理 */ }
    protected override void OnDespawn() { /* 後処理 */ }
}
```

**設計ポイント:**
- `_inPool` フラグで二重返却を防止（`Release()` を誤って2回呼んでも安全）
- `OnSpawn() / OnDespawn()` を `virtual` にし、派生クラスでオーバーライド（Template Method）
- `Action _return` コールバックで返却処理を外部注入（DI的な設計）
- `IFactory` インターフェースで生成を抽象化（Abstract Factory）

---

### ② CSV駆動ノベルシステム — `StoryCsv.cs` / `StoryCueRepo.cs` / `CharacterManager.cs`

`CommentNo`（例：`CHAP1-5`）1つでテキスト・背景・キャラ・BGM・SE・Glow・Fadeが全自動同期。エンジン外から演出を制御。

```csharp
// TextManager.cs：CommentNo を発行するだけ
OnCommentChanged?.Invoke(cno);  // "CHAP1-5" を投げる

// CharacterManager.cs：CSVの1行で演出が全部決まる
StoryCueRepo.TryGet(commentNo, out var row);
BeginCrossFade(bg,   row.BgId,  dur, ref _xfBG);
BeginCrossFade(left, row.LeftId, dur, ref _xfL);
BeginMulFade(_bgMat, PickMulColor(row.GrayBG, row.BlackBG), ...);
BeginGlow(row.Glow == 1, row.Fade, ref _fadeGlow);
sound.PlaySE(row.SEId);
sound.PlayBGMAsync(row.BGMId, loop: true).Forget();
```

**CSVの構造:**

```
// CommuLog.csv（台詞・ルビ）
No, CommentNo, Char, Comment, RubyComment

// StoryCue.csv（演出同期）
CommentNo, BgId, LeftId, BGMId, SEId, Glow, Fade
```

**設計ポイント:**
- JunkShootingで培った「データはコード外で管理する」発想を応用
- `ScriptableObject` だと各自のGitコンフリクトが増えるため、テキスト形式のCSVで全員が同時に編集できる構造に
- コメント番号（CHAP1-5）を変えるだけで演出が切り替わる → エンジニア不在でもプランナーが演出調整可能

---

### ③ シーン遷移フレームワーク — `UIFlowManager.cs` / `SceneTransitionManager.cs`

呼び出し側は `catalog.Get(SceneId.Story)` の1行。フェード・引数渡し・Addressablesが内部で完結。マジックナンバーなし。

```csharp
// UIFlowManager.cs：遷移の呼び出し（1行）
SceneTransitBus.Set("CHAP1", isFade: false);
await transitionManager.LoadSceneAsync(
    catalog.Get(SceneId.Story), payload);  // 文字列直書きなし

// SceneAddressCatalog.cs（ScriptableObject）：シーン名を一元管理
public enum SceneId { Title, Select, Story, Puzzle, Result }
public string Get(SceneId id) {
    for (int i = 0; i < entries.Length; i++)
        if (entries[i].id == id) return entries[i].address;
    return null;
}

// TransitionEffectController.cs：シェーダ _Value + α の二段フェード
public async UniTask PlayOutAsync(CancellationToken ct) {
    await DOTween.To(() => _value, v => material.SetFloat(_valueId, v), 1f, duration)
        .WithCancellation(ct);
    await DOTween.To(() => alpha, v => canvasGroup.alpha = v, 0f, fadeOutDur)
        .WithCancellation(ct);
}
```

**シーン遷移フロー:**
```
UIFlowManager.OnClick_XXX()
  → SceneTransitBus.Set(payload)               // データを静的バスに乗せる
  → TransitionEffectController.PlayOutAsync()  // 暗転
  → SceneTransitionManager.LoadSceneAsync()    // Addressablesでシーン読み込み
  → 遷移先シーンで SceneTransitBus から payload 取り出し
```

---

## 2. システム全体像

### 3層アーキテクチャ

```
┌─────────────────────────────────────────────────────────┐
│                  ゲームUI層（Manager）                    │
│   CharacterManager / SoundManager / TextManager         │
│   UIFlowManager / EnemyManager / GameController         │
│                                                         │
│   ← フレームワーク層のAPIを使用                            │
│   ← ゲームロジック層と疎結合で通信                          │
└─────────────────────────────────────────────────────────┘
         ↓ 使用                        ↑ イベント通知
┌──────────────────────────┐  ┌────────────────────────────┐
│     フレームワーク層       │  │    ゲームロジック層          │
│  ObjectPool<T>           │  │  GridManager               │
│  AddressableUtil         │  │  PieceController           │
│  SceneTransitionManager  │  │  MissionHandler            │
│  StoryCsv / StoryCueRepo │  │  ValueManagement（SO）      │
│  SceneTransitBus         │  │  ShapeData（SO）            │
│  BattleSpeedService      │  │  BattleSpeedController     │
│                          │  │                            │
│  ← ゲーム固有ロジックなし  │  │  ← 上位層に依存しない       │
│  ← 他プロジェクトに転用可  │  │  ← 独立した実装             │
└──────────────────────────┘  └────────────────────────────┘
```

### OnCommentChanged イベントフロー

`TextManager` がイベントを発火し、`CharacterManager` が購読して演出を実行するObserverパターン。

```
TextManager
  └── OnCommentChanged?.Invoke(commentNo)
                │
                ├── CharacterManager（購読）
                │     └── StoryCueRepo.TryGet(commentNo, out row)
                │           ├── BeginCrossFade(bg/left)
                │           ├── BeginGlow / BeginMulFade
                │           └── PlayBGMAsync / PlaySE
                └── （他の購読者があれば追加可能）
```

### Addressablesパイプライン

```
UIFlowManager
  └── catalog.Get(SceneId.Story)           // ScriptableObject でアドレス解決
        └── SceneTransitionManager.LoadSceneAsync(address)
              └── Addressables.LoadSceneAsync(address)
                    └── （失敗時）Resources.Load() フォールバック
```

---

## 3. アーキテクチャ詳細

### 3.1 ディレクトリ構成

```
Assets/
├── AddressableAssetsData/          # Addressables 設定
│   └── AssetGroups/               # グループ定義（Log/Scenes/Audio_BGM/Audio_SE/UI/Font）
│
├── MyAssets/
│   ├── Scripts/
│   │   ├── FrameWork/              # 再利用可能な汎用フレームワーク
│   │   │   ├── Addressable/        # AddressableUtil（ハンドル管理）
│   │   │   ├── ObjectPool/         # ObjectPool<T> + APooledObject + IFactory
│   │   │   ├── Speed/              # BattleSpeedService（ScriptableObject）
│   │   │   ├── TextAndCSV/         # StoryCsv / StoryCueRepo / CommentFormatter
│   │   │   │                       # PartnerCommentCatalog / TextTemplateUtil
│   │   │   ├── Transition/         # SceneTransitionManager / SceneTransitBus
│   │   │   │                       # TransitionEffectController / SceneAddressCatalog
│   │   │   └── Shake/              # ShakeByPerlinNoise
│   │   ├── Manager/                # 各シーンのマネージャー
│   │   │   ├── CharacterManager    # キャラクター演出（クロスフェード・グロー）
│   │   │   ├── SoundManager        # サウンド制御（SoundService への Facade）
│   │   │   ├── TextManager         # ストーリーテキスト・ルビ処理
│   │   │   ├── UIFlowManager       # シーン遷移フロー
│   │   │   ├── EnemyManager        # 敵キャラ表示
│   │   │   ├── GameController      # ゲームループ（UniTask）
│   │   │   └── GridManager         # グリッド状態管理
│   │   └── UI/
│   │       ├── TypingText          # タイピング演出（GC効率的）
│   │       ├── AutoScrollTMPro     # TextMeshPro 自動スクロール
│   │       └── UiAnimationLibrary  # DOTween ラッパー
│   │
│   ├── Shader/                     # BookUI・カスタムシェーダ関連
│   │   ├── BookUI.cs               # ページめくりシェーダ制御
│   │   ├── UITessellator           # UIメッシュ細分化（BaseMeshEffect）
│   │   └── IgnoreMouseInputModule  # キーボード専用入力モジュール
│   │
│   ├── Resources/                  # Addressables と二重管理（※要改善）
│   ├── Sound/BGM / Sound/SE        # 音声アセット
│   ├── Image/                      # 画像・動画アセット
│   ├── Font/                       # フォントアセット
│   └── Scenes/                     # 全シーン
│
└── Komiya/Archive/Script/          # パズルロジック層（旧実装含む）
    ├── ValueManagement             # 全パラメータ管理（ScriptableObject）
    ├── ShapeData                   # ピース形状データ（ScriptableObject）
    ├── GridManager / PieceController / PieceHandler
    ├── MissionHandler / MissionInvoker
    ├── ParameterGauge / DayManagement
    └── BattleSpeedController       # 旧バトルスピード制御（Observer パターン）
```

---

### 3.2 Framework層設計

再利用性・汎用性を意識した層。ゲーム固有のロジックを持たず、別プロジェクトにも転用可能。

#### ObjectPool システム（`FrameWork/ObjectPool/`）

```
IFactory
└── PrefabFactory : IFactory        アセット生成の抽象化（Abstract Factory）

APooledObject : MonoBehaviour       プールオブジェクト基底クラス
└── piece (sealed) : APooledObject  具体実装

ObjectPool<T>                       ジェネリックプール本体
    where T : APooledObject
```

| メソッド | 説明 |
|---------|------|
| `Get()` | プールから取出（なければ Instantiate） |
| `ReturnToPool()` | 返却（`_max` 超過時は破棄） |
| `Prewarm(n)` | n 個を事前生成 |
| `Clear()` | 全破棄 |

#### Addressables ラッパー（`FrameWork/Addressable/AddressableUtil.cs`）

```csharp
// 同一キーへの二重ロードを防ぐハンドル管理
public static async UniTask<T> LoadAssetAsync<T>(string assetKey, CancellationToken ct)
    where T : Object
{
    if (Handles.All(x => x.key != assetKey))          // 未ロードのみ新規発行
        Handles.Add((assetKey, Addressables.LoadAssetAsync<T>(assetKey)));
    ...
}
public static void Dispose()  // IDisposable でハンドル一括解放
```

#### テキスト・CSV 処理（`FrameWork/TextAndCSV/`）

| クラス | 役割 |
|--------|------|
| `StoryCsv` | メインストーリー CSV の非同期読み込み・章別キャッシュ。Addressables → Resources フォールバック |
| `StoryCueRepo` | キャラ表示・BGM・SE・エフェクト設定 CSV。フルパスキーで Addressables 参照 |
| `PartnerCommentCatalog` | パートナーキャラのコメント管理。状態別取得・ランダム選択 |
| `CommentFormatter` | TextMeshProリッチテキストタグを保護しつつ句読点改行ルールを実装 |
| `TextTemplateUtil` | `[分野]` プレースホルダの差分置換。CSV 行分解（ダブルクォート対応） |

#### シーン遷移システム（`FrameWork/Transition/`）

```
SceneTransitBus（静的）   : シーン間データ受け渡しバス（Mediator）
SceneTransitData（SO）    : Payload 構造体定義 { chap, key, intVal, floatVal, strVal, isFade }
SceneTransitionManager    : Addressables.LoadSceneAsync ラッパー
TransitionEffectController: シェーダ _Value + α の二段フェードアニメーション
SceneAddressCatalog（SO） : SceneId enum → アドレス文字列の解決テーブル
```

#### バトルスピード制御（新旧対比）

| | 旧実装 `BattleSpeedController.cs` | 新実装 `BattleSpeedService.cs` |
|--|----------------------------------|-------------------------------|
| 設計 | Singleton + Observer + Strategy | ScriptableObject + C# event |
| 速度計算 | `IFilterableSpeed` フィルターで合成 | `Set() / Pause() / Apply()` |
| 依存 | MonoBehaviour | ScriptableObject（シーン非依存） |
| 特徴 | 購読者リストへ `NotifyAll()` | `OnSpeedChanged` イベント |

---

### 3.3 Manager層設計

#### CharacterManager（`sealed`）

ノベルパートのキャラクター演出を担当。

| 機能 | 実装 |
|------|------|
| クロスフェード | オーバーレイ Image の α を補間。元スプライト → 新スプライト切り替え |
| 乗算色フェード | `Material.SetColor("_MulColor")` でグレーアウト・ブラックアウト |
| グロー演出 | `_OverallAlpha / _Glow` シェーダプロパティ操作。自動進行イベント付き |
| キャンセル管理 | 演出ごとに `CancellationTokenSource` を独立保持 |

```csharp
// マテリアルインスタンス化で共有マテリアルへの汚染を防止
_mat = Instantiate(baseMat);
_mat.SetColor(MulColorId, targetColor);
```

#### SoundManager / SoundService

```
SoundManager（MonoBehaviour, sealed）   ← 外部向け Facade
    └── SoundService（ScriptableObject, sealed）  ← 実装本体
```

| 機能 | 実装詳細 |
|------|---------|
| BGM クロスフェード | AudioSource 2系統を手動 Lerp で切り替え（DOTween 非依存） |
| SE 多重再生 | AudioSource プールから空きソースを割り当て |
| ポーズ対応 | `Time.unscaledDeltaTime` でゲームポーズ中も再生継続 |

#### TextManager（`sealed`）

| 機能 | 実装 |
|------|------|
| ルビ解析 | `{漢字\|かな}` / `｜漢字《かな》` の2形式に対応する正規表現 |
| 漢字自動ルビ | 漢字ブロック単位でカタカナ読みを抽出・ひらがな正規化 |
| 先読み | 次章の CSV を非同期で先読みし表示ラグを排除 |
| イベント | `OnCommentChanged` で CharacterManager にキャラ更新を通知 |

---

### 3.4 UI層設計

| クラス | 説明 |
|--------|------|
| `TypingText` | `maxVisibleCharacters` による GC 効率的なタイピング演出。キー参照 or 直接テキスト両対応 |
| `AutoScrollTMPro` | コンテンツ高さ > ビューポート高さ のときのみ自動スクロール有効化 |
| `UiAnimationLibrary` | DOTween による Fade・MaterialFloat アニメーションの静的ユーティリティ |
| `BookUI` | SmoothStep + シェーダでページめくり演出。Canvas スケーラ対応 |
| `UITessellator` | `BaseMeshEffect` 継承。三角形を再帰4分割してシェーダ用に頂点密度を上げる |
| `IgnoreMouseInputModule` | `BaseInputModule` 継承。マウス入力を遮断してキーボード・ジョイスティックのみ処理 |

---

### 3.5 GameLogic層（Archive）

`Assets/Komiya/Archive/Script/` 以下。パズルゲーム固有ロジック。

| クラス | 役割 |
|--------|------|
| `ValueManagement`（SO） | 親子パラメータ・タイマー・日数・クリア状態を一元管理 |
| `ShapeData`（SO） | ピース形状（`List<Vector2Int>`）・表示文字・パラメータ変動値を保持 |
| `GridManager` | 2次元配列でグリッド状態管理。座標変換・配置判定・登録解除 |
| `PieceController` | `OnMouseDown/Drag/Up` によるドラッグ操作とグリッドスナップ |
| `MissionHandler` | ランダムミッション選択・達成判定。`CallShader() / CallEnemy()` は未実装 |
| `ParameterGauge` | `RectTransform.sizeDelta` によるゲージ高さ制御 |
| `BattleSpeedController` | 旧実装。Observer + フィルター戦略による速度合成（新実装に移行中） |

---

### 3.6 外部パッケージ依存

| パッケージ | 用途 | 主な使用箇所 |
|-----------|------|------------|
| **Cysharp.Threading.Tasks (UniTask)** | 非同期処理全般 | ほぼ全ファイル |
| **DOTween** | Tween アニメーション | `UiAnimationLibrary`, `ShakeByPerlinNoise` |
| **TextMeshPro** | テキスト表示・ルビ | `TextManager`, `TypingText`, `piece` |
| **UnityEngine.AddressableAssets** | アセット非同期管理 | `AddressableUtil`, `SceneTransitionManager`, `UIFlowManager` |
| **UnityEngine.UI** | UI 基本コンポーネント | 多数 |
| **UnityEngine.EventSystems** | UI 入力処理 | `IgnoreMouseInputModule`, `PageFlickHandler` |
| **UI Extensions (BookUI)** | ページめくり UI | `BookUI`, `UITessellator` |
| **Unity.VisualScripting** | （インポートのみ） | `DayManagement`, `MissionInvoker` |

---

### 3.7 GoFデザインパターン対応表（23パターン）

#### 生成パターン（Creational）

| # | パターン | 該当 | 実装箇所 | 説明 |
|---|---------|:----:|---------|------|
| 1 | **Abstract Factory** | ✅ | `IFactory` インターフェース | `Create()` と `PoolParent` を規定し、具体ファクトリ（PrefabFactory）が実装 |
| 2 | **Builder** | △ | `ObjectPool` コンストラクタ | `initial / max` を受け取る段階的初期化（厳密な Builder ではない） |
| 3 | **Factory Method** | ✅ | `PrefabFactory.Create()` | サブクラスがオブジェクト生成方法を決定 |
| 4 | **Prototype** | △ | `TransitionEffectController` | `Material.Instantiate()` で複製し共有汚染を防止 |
| 5 | **Singleton** | ✅ | `GameController`, `GridManager`, `StoryCsv`, `SceneTransitBus`, `BattleSpeedController` | `static Instance` / `static readonly` パターン |

#### 構造パターン（Structural）

| # | パターン | 該当 | 実装箇所 | 説明 |
|---|---------|:----:|---------|------|
| 6 | **Adapter** | ✅ | `IgnoreMouseInputModule` | 既存 UI システムにキーボード専用入力を適合させる |
| 7 | **Bridge** | △ | `SoundManager` + `SoundService` | 実装（Service）と抽象（Manager）を分離 |
| 8 | **Composite** | △ | `ShapeData.Cells (List<Vector2Int>)` | ピース形状を複数セルの集合として扱う |
| 9 | **Decorator** | ✅ | `UITessellator (BaseMeshEffect継承)` | 既存 UI コンポーネントに頂点細分化機能を動的付加 |
| 10 | **Facade** | ✅ | `SoundManager → SoundService` | 複雑な AudioSource 管理を `PlayBGM / PlaySE` で隠蔽 |
| 11 | **Flyweight** | ✅ | `ObjectPool<T>` | オブジェクトを使い回してメモリ・生成コストを削減 |
| 12 | **Proxy** | ✅ | `SceneTransitBus` | シーン間の直接参照を排除し、静的バスがデータ受け渡しを代理 |

#### 振る舞いパターン（Behavioral）

| # | パターン | 該当 | 実装箇所 | 説明 |
|---|---------|:----:|---------|------|
| 13 | **Chain of Responsibility** | △ | `ChildClickForwarder` | 子オブジェクトのマウスイベントを `SendMessage` で親へ連鎖転送 |
| 14 | **Command** | ✅ | `MissionInvoker`, `BattleSpeedEntry` | 処理の起動者と処理本体を分離。`CancellationTokenSource` でキャンセルも管理 |
| 15 | **Interpreter** | △ | `CommentFormatter`, `TextManager.ParseInlineRuby()` | ルビ構文（`{漢字\|かな}`）の独自文法を解釈・変換 |
| 16 | **Iterator** | △ | `StoryCsv.GetLines()` | `List<StoryLine>` の LINQ による列挙 |
| 17 | **Mediator** | ✅ | `SceneTransitBus` | 複数シーン間の直接依存をなくし、バスが中継 |
| 18 | **Memento** | △ | `SceneTransitBus.Payload` | シーン遷移時の状態スナップショット保存 |
| 19 | **Observer** | ✅ | `BattleSpeedController.Subscribe/NotifyAll`, `TextManager.OnCommentChanged`, `CharacterManager.OnGlowAutoAdvance` | C# event / delegate による通知 |
| 20 | **State** | ✅ | `BattleSpeedService`（通常/ポーズ）, `APooledObject._inPool` | 状態に応じて振る舞いを変更 |
| 21 | **Strategy** | ✅ | `BattleSpeedController.IFilterableSpeed` | 速度フィルター計算ロジックを差し替え可能なインターフェースで抽象化 |
| 22 | **Template Method** | ✅ | `APooledObject.OnSpawn() / OnDespawn()` | 基底クラスがライフサイクルの骨格を定義し、派生クラス（`piece`）が詳細を実装 |
| 23 | **Visitor** | △ | `UITessellator.ModifyMesh()` | 頂点ストリームを走査して各頂点を操作 |

> **凡例:** ✅ = GoFの定義に忠実な実装 / △ = 役割・意図が対応するが厳密なGoF実装ではない

---

### 3.8 Addressables構成

#### グループ一覧

| グループ | 収録内容 | 配置パス |
|---------|---------|---------|
| **Log** | CSV・ScriptableObject・Mixer | `Assets/MyAssets/Resources/` |
| **Scenes** | 全シーン（8本） | `Assets/MyAssets/Scenes/` |
| **Audio_BGM** | BGM × 7（mp3） | `Assets/MyAssets/Sound/BGM/` |
| **Audio_SE** | SE × 19（mp3 / wav 混在） | `Assets/MyAssets/Sound/SE/` |
| **UI** | 画像・動画（png / jpg / svg / mp4 / gif） | `Assets/MyAssets/Image/` |
| **Font** | フォント 2 種 + txt | `Assets/MyAssets/Font/` |
| **Default Local Group** | **空**（デフォルトグループ・未使用） | — |

#### アドレスキー参照方式（3種混在）

| ファイル | キー | 種別 |
|---------|------|------|
| `StoryCsv.cs` | `"CommuLog"` | ラベルキー（Log グループの CommuLog.csv に付与） |
| `StoryCueRepo.cs` | `"Assets/MyAssets/Resources/StoryCue.csv"` | フルパスキー |
| `PartnerCommentCatalog.cs` | `"CSV/PartnerComments"` | 独自キー（**Addressables 未登録**） |
| `TypingText.cs` | `AssetReference csvAsset` | インスペクタ参照 |

#### フォールバック設計

`StoryCsv` と `PartnerCommentCatalog` は Addressables 失敗時に `Resources.Load()` へフォールバックする設計になっている。

```csharp
// StoryCsv.cs の例
try {
    var h = Addressables.LoadAssetAsync<TextAsset>("CommuLog");
    ta = await h.Task;
}
catch { /* Addressables 失敗を捕捉 */ }

if (ta == null)
    ta = Resources.Load<TextAsset>("CommuLog");  // Resources フォールバック
```

---

### 3.9 既知の問題・要改善点

| 優先度 | 問題 | 場所 | 対応方針 |
|--------|------|------|---------|
| 高 | `Resources/` との二重管理 | Log グループ全体 | アセットを `Resources/` 外に移動し Addressables 一本化 |
| 高 | `"CSV/PartnerComments"` が Addressables 未登録 | `PartnerCommentCatalog.cs:98` | CSV を Addressables に追加 or Addressables 呼び出しを削除 |
| 中 | アドレスキーが 3 種混在 | StoryCsv / StoryCueRepo / PartnerCommentCatalog | フルパス or ラベルに統一（`SceneAddressCatalog` 方式推奨） |
| 中 | デバッグ用キー入力がゲーム内に残存 | `ParameterGauge.cs`（KeyCode.I/O/K/L） | `#if UNITY_EDITOR` ガードまたは削除 |
| 中 | `MissionHandler` の未実装メソッド | `CallShader()`, `CallEnemy()` | 実装またはインターフェース化 |
| 低 | `PageFlickHandler` のコード重複 | `OnPointerUp()` 35–46 行 | 共通メソッドに抽出 |
| 低 | 命名タイポ | `Paramete`, `ParamateInitializer`, `isParentl` | リネーム |
| 低 | `BookManager`, `ResultManager` が空実装 | 各 cs ファイル | 実装 or 削除 |

#### 改善済み（旧→新リファクタリング）

| 旧実装 | 新実装 | 変更内容 |
|--------|--------|---------|
| `BattleSpeedController.cs`（MonoBehaviour + Observer） | `BattleSpeedService.cs`（ScriptableObject + event） | シーン非依存化・依存関係の簡略化 |
| `DB_Image.cs`（Resources.Load ベース） | `ImageAddressCatalog.cs`（ScriptableObject） | インスペクタ管理への移行 |

---

*このドキュメントは `PiecePeace_architecture.md` として管理しています。コード変更時は対応するセクションを更新してください。*
