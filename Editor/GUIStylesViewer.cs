// Made by Neonagee to make your life better ^_^ https://github.com/Neonagee/GUIStylesViewer

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

class GUIStylesViewer : EditorWindow
{
    static GUIStylesViewer window;

    [System.Serializable]
    public class Style
    {
        public readonly GUIStyle style;
        public readonly GUIContent content;

        public readonly string nameToLower;
        public readonly string filteredName;

        public readonly Vector2 size;
        public float heightWithContent;
        public float height;
        public float width;
        public float spaceBehind;
        public float currentHeight;
        public float yPosition;
        public bool isLabel = true;

        public bool pressed;
        public Style(GUIStyle Style, GUIContent Content, Vector2 Size, float Height)
        {
            style = Style;
            content = Content;

            nameToLower = Content.text.ToLower();
            filteredName = nameToLower + " ";
            filteredName += Style.normal.background?.name.ToLower() + " ";
            filteredName += Style.focused.background?.name.ToLower() + " ";
            filteredName += Style.hover.background?.name.ToLower() + " ";
            filteredName += Style.active.background?.name.ToLower() + " ";
            filteredName += Style.onNormal.background?.name.ToLower() + " ";
            filteredName += Style.focused.background?.name.ToLower() + " ";
            filteredName += Style.hover.background?.name.ToLower() + " ";
            filteredName += Style.onActive.background?.name.ToLower() + " ";

            size = Size;
            height = Height;
            if (style.active.background != null)
            {
                height = style.active.background.height;
                width = style.active.background.width;
                isLabel = false;
            }
            if (style.hover.background != null)
            {
                height = style.hover.background.height;
                width = style.hover.background.width;
                isLabel = false;
            }
            if (style.normal.background != null)
            {
                height = style.normal.background.height;
                width = style.normal.background.width;
                isLabel = false;
            }
            if (height < 8 || width < 8)
            {
                height = 8;
                width = 8;
            }
        }
    }
    const string sampleText = "Sample Text";
    readonly GUIContent sampleContent;
    List<Style> styles = new List<Style>();
    public bool showSampleText;
    public Color backgroundColor = Color.white;
    public Color contentColor = Color.white;
    public bool useCustomWidth;
    public float customWidth = 100;
    public bool optimizeScroll = true;
    public bool grayOnWhite = true;
    float backgroundHeight;
    AnimBool settingsCollapse;

    public GUIStylesViewer()
    {
        sampleContent = new GUIContent("Sample Text");
    }

    [UnityEditor.MenuItem("Window/UI/GUIStyles Viewer")]
    public static void OpenWindow()
    {
        window = GetWindow<GUIStylesViewer>();
        string iconGUID = AssetDatabase.FindAssets("GUIStylesViewerIcon")[0];
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(iconGUID));
        window.titleContent = new GUIContent("GUIStyles Viewer", icon);
    }
    void OnEnable()
    {
        regionBg = null;
        settingsCollapse = new AnimBool(Repaint) { speed = 3.5f };
        LoadGUISkins();
    }
    GUIStyle regionBg;
    GUIStyle settingsBox;
    GUIStyle foldout;
    GUIStyle searchTextField;
    GUIStyle searchCancelButton;
    GUIStyle richLabel;
    string searchFilter = "";
    void GetGUIStyles()
    {
        regionBg = new GUIStyle("RegionBg")
        {
            margin = new RectOffset(8, 8, 8, 0),
            padding = new RectOffset(16, 16, 8, 16),
            border = new RectOffset(12, 12, 12, 12),
            contentOffset = new Vector2(10, -10),
            fontSize = 12,
            alignment = TextAnchor.UpperLeft
        };
        backgroundHeight = regionBg.CalcSize(new GUIContent(" ")).y;
        settingsBox = new GUIStyle("NotificationBackground")
        {
            margin = new RectOffset(8, 8, 8, 8),
            padding = new RectOffset(8, 8, 8, 8),
            border = new RectOffset(12, 12, 12, 12),
            contentOffset = new Vector2(10, -10),
            fontSize = 12,
            alignment = TextAnchor.UpperLeft
        };
        foldout = new GUIStyle("Foldout") { fontSize = 12 };
        searchTextField = new GUIStyle("ToolbarSeachTextField");
        searchCancelButton = new GUIStyle("ToolbarSeachCancelButton");
        richLabel = new GUIStyle(EditorStyles.label) { richText = true };
    }
    //GUISkin customSkin;
    void LoadGUISkins()
    {
        GUISkin inspectorSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
        styles.Clear();
        foreach (GUIStyle m_style in inspectorSkin)
        {
            var style = new Style
                (
                    m_style, // GUIStyle
                    new GUIContent(m_style.name), // GUIContent
                    m_style.CalcSize(GUIContent.none), // Size
                    m_style.CalcHeight(new GUIContent(sampleText), customWidth) // Size With Content
                );
            styles.Add(style);
        }
        RecalculateStylesHeight();
    }
    void RecalculateStylesHeight()
    {
        for (int i = 0; i < styles.Count; i++)
        {
            float width = useCustomWidth ? customWidth : styles[i].isLabel ? position.width / 1.5f : styles[i].width;

            styles[i].heightWithContent = styles[i].style.CalcHeight(new GUIContent(sampleText), width);
        }
    }
    Vector2 scrollPos;
    public struct VisibleItem
    {
        public float LastYPos;
        public int Index;
        public float InvisibleYSpace;
    }
    float GetStyleHeight(Style style)
    {
        return style.height + (showSampleText ? style.heightWithContent : 0) + backgroundHeight;
    }
    VisibleItem GetFirstVisibleItem(List<Style> stylesList, Vector2 scrollPos)
    {
        float yPos = 0;
        float invisibleSpace = 0;
        for (int i = 0; i < stylesList.Count; i++)
        {
            yPos += stylesList[i].currentHeight;
            if (stylesList[i].yPosition > scrollPos.y)
            {
                return new VisibleItem() { Index = i, LastYPos = yPos, InvisibleYSpace = invisibleSpace };
            }
            invisibleSpace += stylesList[i].currentHeight;
        }
        return default;
    }
    VisibleItem GetLastVisibleItem(List<Style> stylesList, VisibleItem firstItem, Vector2 scrollPos, float scrollHeight)
    {
        float yPos = firstItem.LastYPos;
        VisibleItem lastItem = new VisibleItem();
        for (int i = firstItem.Index; i < stylesList.Count; i++)
        {
            if (stylesList[i].yPosition > (scrollPos.y + (scrollHeight)) )
            {
                if (lastItem.Index == 0)
                {
                    lastItem.Index = Mathf.Clamp(i + 1, 0, stylesList.Count - 1);
                    lastItem.LastYPos = yPos;
                }
                lastItem.InvisibleYSpace += stylesList[i].currentHeight;
            }
            yPos += stylesList[i].currentHeight;
        }
        if (lastItem.Index == 0)
        {
            lastItem.Index = stylesList.Count;
            lastItem.InvisibleYSpace = 10;
        }
        return lastItem;
    }
    float GetAllElementsHeight(List<Style> stylesList)
    {
        return stylesList[stylesList.Count - 1].spaceBehind + stylesList[stylesList.Count - 1].currentHeight;
    }
    void CalculateStylesLayout(List<Style> stylesList)
    {
        RecalculateStylesHeight();
        float yPos = 0;
        for (int i = 0; i < stylesList.Count; i++)
        {
            stylesList[i].currentHeight = GetStyleHeight(stylesList[i]);
            stylesList[i].spaceBehind = yPos;
            yPos += stylesList[i].currentHeight;
            stylesList[i].yPosition = stylesList[i].spaceBehind + stylesList[i].currentHeight;
        }
    }
    VisibleItem FirstItem;
    VisibleItem LastItem;
    int scrollStart;
    int scrollEnd;
    Vector2 lastScrollPos;
    void OnGUI()
    {
        if (regionBg == null)
            GetGUIStyles();
        Color m_backgroundColor = GUI.backgroundColor;
        Color m_contentColor = GUI.contentColor;
        float labelWidth = EditorGUIUtility.labelWidth;
        bool isLayoutEvent = Event.current.type == EventType.Layout;

        // Filter styles by search

        List<Style> filteredStyles = new List<Style>();
        for (int i = 0; i < styles.Count; i++)
        {
            if (styles[i].filteredName.Contains(searchFilter.ToLower()))
                filteredStyles.Add(styles[i]);
        }
        // Get start and end items for optimized scroll
        if (isLayoutEvent)
        {
            CalculateStylesLayout(filteredStyles);
            FirstItem = GetFirstVisibleItem(filteredStyles, scrollPos);
            LastItem = GetLastVisibleItem(filteredStyles, FirstItem, scrollPos, position.height);
            scrollStart = optimizeScroll ? FirstItem.Index : 0;
            scrollEnd = (optimizeScroll ? LastItem.Index : filteredStyles.Count);
        }

        GUILayout.Space(-6);
        EditorGUILayout.BeginVertical(regionBg);
        GUILayout.Space(4);
        settingsCollapse.target = GUILayout.Toggle(settingsCollapse.target, "Settings", foldout);
        GUILayout.Space(2);
        if (EditorGUILayout.BeginFadeGroup(settingsCollapse.faded))
        {
            EditorGUIUtility.labelWidth = 95;
            showSampleText = EditorGUILayout.Toggle("Show Text", showSampleText);
            EditorGUI.BeginChangeCheck();

            // Custom width slider
            EditorGUILayout.BeginHorizontal();
            useCustomWidth = EditorGUILayout.Toggle("Custom Width", useCustomWidth);
            EditorGUI.BeginDisabledGroup(!useCustomWidth);
            customWidth = (int)Mathf.Clamp(EditorGUILayout.Slider(customWidth, 10, position.width / 1.5f), 10, position.width / 1.5f);
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
            if (EditorGUI.EndChangeCheck())
            {
                RecalculateStylesHeight();
                Repaint();
            }

            // Custom colors
            backgroundColor = EditorGUILayout.ColorField("Background", backgroundColor);
            contentColor = EditorGUILayout.ColorField("Content", contentColor);
            EditorGUIUtility.labelWidth = labelWidth;
            grayOnWhite = GUILayout.Toggle(grayOnWhite, " Gray Background On White Label");

            // Optimized scroll toggle
            EditorGUILayout.BeginHorizontal();
            optimizeScroll = GUILayout.Toggle(optimizeScroll, 
                new GUIContent(" Optimized Scroll" + (!optimizeScroll ? " (Experimental)" : ""), optimizeScroll ? "Experimental." : ""), 
                GUILayout.Width(optimizeScroll ? 120 : 210));

            if (optimizeScroll)
                EditorGUILayout.LabelField("Start: " + scrollStart + " : End: " + scrollEnd, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(6);
        }
        EditorGUILayout.EndFadeGroup();

        // Search field
        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 45;
        EditorGUILayout.LabelField("Count: " + filteredStyles.Count, EditorStyles.miniLabel, GUILayout.MinWidth(45));
        EditorGUIUtility.labelWidth = labelWidth;
        searchFilter = EditorGUILayout.TextField(searchFilter, searchTextField, GUILayout.MinWidth(10));
        GUILayout.Space(-1);
        GUILayout.Button("", "ToolbarSeachCancelButtonEmpty");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        try
        {
            if (optimizeScroll)
                GUILayout.Space(FirstItem.InvisibleYSpace);
        }
        catch { }

        for (int i = scrollStart; i < scrollEnd; i++)
        {
            GUIStyle style = filteredStyles[i].style;
            bool isLabel = filteredStyles[i].isLabel;
            bool pressable = style.onNormal.background;
            bool isPressed = filteredStyles[i].pressed;
            try // Need try catch to disable stupid GUI errors
            {
                GUI.backgroundColor = grayOnWhite && (isLabel || showSampleText) ? 
                    Color.Lerp(backgroundColor, Color.gray, style.normal.textColor.grayscale) : backgroundColor;
                GUI.contentColor = contentColor;

                string labelHex = ColorUtility.ToHtmlStringRGB(Color.gray * GUI.backgroundColor);
                EditorGUILayout.BeginVertical(regionBg);
                GUI.backgroundColor = new Color(GUI.backgroundColor.r, GUI.backgroundColor.g, GUI.backgroundColor.b, 1);

                if (GUILayout.Button("<color=#" + labelHex + ">" + (i + 1) + "</color>  " + filteredStyles[i].content.text +
                    (pressable ? "<color=#" + labelHex + "> Pressable</color>" : ""), 
                    richLabel, GUILayout.MinWidth(0)))
                {
                    GenericMenu menu = new GenericMenu();
                    int m_i = i;

                    GUIContent content = new GUIContent("Copy Name");
                    menu.AddItem(content, false, () =>
                    {
                        EditorGUIUtility.systemCopyBuffer = filteredStyles[m_i].content.text;
                    });

                    content = new GUIContent("Save Texture/Normal");
                    if (style.normal.background)
                        menu.AddItem(content, pressable ? !isPressed : false, () => { SaveGUIStateTexture(style.normal); }); else menu.AddDisabledItem(content);

                    content = new GUIContent("Save Texture/Focused");
                    if (style.focused.background)
                        menu.AddItem(content, false, () => { SaveGUIStateTexture(style.focused); }); else menu.AddDisabledItem(content);

                    content = new GUIContent("Save Texture/Hover");
                    if (style.hover.background)
                        menu.AddItem(content, false, () => { SaveGUIStateTexture(style.hover); }); else menu.AddDisabledItem(content);

                    content = new GUIContent("Save Texture/Active");
                    if (style.active.background)
                        menu.AddItem(content, false, () => { SaveGUIStateTexture(style.active); }); else menu.AddDisabledItem(content);

                    // Seperator
                    menu.AddSeparator("Save Texture/");

                    content = new GUIContent("Save Texture/OnNormal");
                    if (style.onNormal.background)
                        menu.AddItem(content, pressable ? isPressed : false, () => { SaveGUIStateTexture(style.onNormal); });  else menu.AddDisabledItem(content);

                    content = new GUIContent("Save Texture/OnFocused");
                    if (style.onFocused.background)
                        menu.AddItem(content, false, () => { SaveGUIStateTexture(style.onFocused); }); else menu.AddDisabledItem(content);

                    content = new GUIContent("Save Texture/OnHover");
                    if (style.onHover.background)
                        menu.AddItem(content, false, () => { SaveGUIStateTexture(style.onHover); }); else menu.AddDisabledItem(content);

                    content = new GUIContent("Save Texture/OnActive");
                    if (style.onActive.background)
                        menu.AddItem(content, false, () => { SaveGUIStateTexture(style.onActive); });
                    else menu.AddDisabledItem(content);

                    menu.ShowAsContext();
                }
                GUILayout.Space(10);
                float height = filteredStyles[i].height;
                float heightWithContent = filteredStyles[i].heightWithContent;
                float width = filteredStyles[i].width;
                float maxWidth = isLabel ? position.width / 1.5f : width;

                filteredStyles[i].pressed = GUILayout.Toggle
                (
                    filteredStyles[i].pressed,

                    (showSampleText || isLabel ? sampleContent : GUIContent.none), style,

                    GUILayout.ExpandWidth(true),   
                    GUILayout.MinWidth( isLabel ? position.width / 1.5f : width),
                    GUILayout.MaxWidth( useCustomWidth ? customWidth : maxWidth), 
                    GUILayout.Height(height + (showSampleText || isLabel ? heightWithContent : 0))
                );

                GUI.backgroundColor = m_backgroundColor;
                GUI.contentColor = m_contentColor;

                EditorGUILayout.EndVertical();
            }
            catch { }
        }
        try
        {
            if (optimizeScroll)
                GUILayout.Space(LastItem.InvisibleYSpace);
        }
        catch { }
        EditorGUILayout.EndScrollView();

        if (optimizeScroll && lastScrollPos != scrollPos)
        {
            lastScrollPos = scrollPos;
            Repaint();
        }

        GUI.backgroundColor = m_backgroundColor;
        GUI.contentColor = m_contentColor;
    }
    void SaveGUIStateTexture(GUIStyleState state)
    {
        string savePath;
        if(!(savePath = EditorUtility.SaveFilePanel("Save GUI Texture", EditorApplication.applicationPath, 
            state.background.name, "png")).IsNullOrEmpty())
        {
            Texture2D texture = state.background;

            // https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-?_ga=2.80016279.1816154821.1570372156-2116389004.1565795589
            // Create a temporary RenderTexture of the same size as the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                                texture.width,
                                texture.height,
                                0,
                                RenderTextureFormat.Default,
                                RenderTextureReadWrite.Linear);
            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(texture, tmp);

            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;

            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;

            // Create a new readable Texture2D to copy the pixels to it
            Texture2D readableTexture = new Texture2D(texture.width, texture.height);

            // Copy the pixels from the RenderTexture to the new Texture
            readableTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            readableTexture.Apply();

            // Reset the active RenderTexture
            RenderTexture.active = previous;

            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);

            byte[] pngEncode = readableTexture.EncodeToPNG();
            System.IO.File.WriteAllBytes(savePath, pngEncode);
        }
    }
}
