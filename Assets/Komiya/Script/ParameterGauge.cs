using UnityEngine;
using UnityEngine.UI;

using Value;
namespace Paramete
{
    public class ParameterGauge : MonoBehaviour
    {




        [Header(" l Ǘ  f [ ^[ValueManagement]  ScriptableObject")]
        [SerializeField] private ValueManagement valueManagement;

        [Header(" q  or e ̃p     [ ^ 𔽉f    UI.Image")]
        [SerializeField] private Image parentGauge;
        [SerializeField] private Image childGauge;

        private float maxHeight;
        private int maxParameter;

        private void Start()
        {

            InitializeParamater();




            if (valueManagement == null)
            {
                valueManagement = GetComponent<ValueManagement>();
                maxParameter = valueManagement.MaxParameter;
            }


            if (parentGauge != null)
            {
                maxHeight = parentGauge.rectTransform.sizeDelta.y;
            }
            else
            {
                Debug.LogError("ParentGauge   ݒ肳  Ă  ܂   I");
                return;
            }
            maxParameter = valueManagement.MaxParameter;
            ChangeGauge(valueManagement.ParentParameter, parentGauge);
            ChangeGauge(valueManagement.ChildParameter, childGauge);
        }

        private void Update()
        {

            if (Input.GetKeyDown(KeyCode.I))
            {
                valueManagement.ParentParameter++;
                if (valueManagement.ParentParameter >= maxParameter) valueManagement.ParentParameter = maxParameter;

                ChangeGauge(valueManagement.ParentParameter, parentGauge);
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                valueManagement.ChildParameter++;
                if (valueManagement.ChildParameter >= maxParameter) valueManagement.ChildParameter = maxParameter;

                ChangeGauge(valueManagement.ChildParameter, childGauge);
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                valueManagement.ParentParameter--;
                if (valueManagement.ParentParameter <= 0) valueManagement.ParentParameter = 0;

                ChangeGauge(valueManagement.ParentParameter, parentGauge);
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                valueManagement.ChildParameter--;
                if (valueManagement.ChildParameter <= 0) valueManagement.ChildParameter = 0;

                ChangeGauge(valueManagement.ChildParameter, childGauge);
            }
        }


        public void ChangeGauge(int Parameter, Image TargetImage)
        {

            if (TargetImage == null)
            {

                Debug.LogError("TargetImage  Null ł ");
                return;
            }

            RectTransform rectTransform = TargetImage.rectTransform;
            Vector2 size = rectTransform.sizeDelta;


            float currentParameter = Mathf.Clamp(Parameter, 0, maxParameter);


            float ratio = currentParameter / (float)maxParameter;


            float newHeight = maxHeight * ratio;


            size.y = newHeight;


            rectTransform.sizeDelta = size;
        }

        /// <summary>
        /// 値の初期化
        /// </summary>
        public void InitializeParamater()
        {
            Debug.LogWarning("パラメーターを初期化しました");
            valueManagement.ParentParameter = valueManagement.InitialParentParamater;
            valueManagement.ChildParameter = valueManagement.InitialChildParamater;
        }

        /// <summary>
        /// 親ゲージのImageコンポーネントを取得
        /// </summary>
        /// <returns>親ゲージのImageコンポーネント</returns>
        public Image GetParentGaugeImage()
        {
            return parentGauge;
        }

        /// <summary>
        /// 子ゲージのImageコンポーネントを取得
        /// </summary>
        /// <returns>子ゲージのImageコンポーネント</returns>
        public Image GetChildGaugeImage()
        {
            return childGauge;
        }
    }
}