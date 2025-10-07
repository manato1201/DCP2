﻿using UnityEngine;

namespace OpalStudio.CustomToolbar.Editor.ToolbarElements
{
      sealed internal class ToolbarSpace : BaseToolbarElement
      {
            protected override string Name => "Layout Space";

            public override void OnDrawInToolbar()
            {
                  GUILayout.Space(25);
            }
      }
}