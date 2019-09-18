using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Archtoolkit.ATImport.Utils;

namespace Archtoolkit.ATImport
{
    [System.Serializable]
    public class ATImportWindowBase : EditorWindow
    {

        protected Rect header;

        protected Texture2D logo;

        protected Texture2D background;

        protected GUIStyle backgroundStyle;

        private void OnEnable()
        {
            EditorUtility.SetDirty(this);
        }

        protected void CreateLogoAndBackground(string logoPath, float headerHeight = 85f)
        {
            this.header.x = -1;
            this.header.y = 5;

            this.header.width = this.position.width;
            this.header.height = headerHeight;

            this.logo = new Texture2D(1, 1);
            this.logo.SetPixel(0, 0, new Color(125, 124, 32, 1));
            this.logo.Apply();

            this.logo = Resources.Load<Texture2D>(logoPath);
            if (!EditorGUIUtility.isProSkin)
                this.background = TextureUtils.MakeTex(1, 1, Color.gray);
            else
                this.background = TextureUtils.MakeTex(1,1,new Color32(69,69,69,255));
        }

        protected void ApplyLogo(float headerHeight = 85f)
        {
            // Create Style for Main logo
            this.backgroundStyle = new GUIStyle();
            backgroundStyle.alignment = TextAnchor.MiddleCenter;
            backgroundStyle.normal.background = background;

            var logoStyle = new GUIStyle();
            logoStyle.alignment = TextAnchor.MiddleCenter;

            if (this.logo == null || this.background == null)
                this.CreateLogoAndBackground(ATImportDataPath.WINDOW_LOGO_PATH, headerHeight);

            GUILayout.BeginArea(new Rect(0,5,this.position.width,headerHeight), new GUIContent(this.logo),logoStyle);
            // Apply main logo
            //GUI.DrawTexture(this.header, this.logo, ScaleMode.ScaleToFit);

            GUILayout.EndArea();
        }
    }
}

