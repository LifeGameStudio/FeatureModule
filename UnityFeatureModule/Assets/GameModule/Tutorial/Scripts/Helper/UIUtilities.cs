namespace GameModule.Tutorial.Scripts.Helper {
    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public enum AnchorPreset {
        TopLeft,
        TopCenter,
        TopRight,
        TopStretch,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        MiddleStretch,
        BottomLeft,
        BottomCenter,
        BottomRight,
        BottomStretch,
        StretchLeft,
        StretchCenter,
        StretchRight,
        StretchFull
    }
    
    /// <summary>
    /// Extension methods for debugging purpose.
    /// </summary>
    public static class UIUtilities {
        /// <summary>
        /// Conveniently add a child recttransform.
        /// </summary>
        /// <param name="childName">Name of the child object.</param>
        public static RectTransform AddChild(this RectTransform r, string childName) {
            var gobj = new GameObject(childName);
            gobj.transform.SetParent(r, false);
            gobj.AddComponent(typeof(CanvasRenderer));
            return gobj.AddComponent<RectTransform>();
        }

        public static RectTransform SetColor(this RectTransform r, Color color) {
            r.GetComponent<Image>().color = color;
            return r;
        }
        

        /// <summary>
        /// Conveniently add a child recttransform.
        /// </summary>
        /// <param name="childName">Name of the child object.</param>
        public static RectTransform AddChild(this UIBehaviour b, string childName) {
            var gobj = new GameObject(childName);
            gobj.transform.SetParent(b.transform, false);
            gobj.AddComponent(typeof(CanvasRenderer));
            return gobj.AddComponent<RectTransform>();
        }

        /// <summary>
        /// Conveniently get parent transform as RectTransform.
        /// </summary>
        public static RectTransform ParentRect(this RectTransform r) {
            return r.parent == null ? null : r.parent.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Conveniently set recttransform pivot.
        /// </summary>
        /// <param name="pivot">Pivot point</param>
        public static RectTransform SetPivot(this RectTransform r, Vector2 pivot) {
            r.pivot = pivot;
            return r;
        }
        
        /// <summary>
        /// Conveniently set recttransform pivot.
        /// </summary>
        public static RectTransform SetPivot(this RectTransform r, float pivotX, float pivotY) {
            r.pivot = new Vector2(pivotX, pivotY);
            return r;
        }

        /// <summary>
        /// Conveniently set recttransform size.
        /// </summary>
        /// <param name="size">Size to set.</param>
        public static RectTransform SetSize(this RectTransform r, Vector2 size) {
            r.sizeDelta = size;
            return r;
        }
        
        /// <summary>
        /// Conveniently set recttransform size.
        /// </summary>
        /// <param name="size">Size to set.</param>
        public static RectTransform SetSize(this RectTransform r, float sizeX, float sizeY) {
            r.sizeDelta = new Vector2(sizeX, sizeY);
            return r;
        }
        
        /// <summary>
        /// Conveniently set recttransform anchored position.
        /// </summary>
        /// <param name="position">Anchored position to set.</param>
        public static RectTransform SetAnchoredPosition(this RectTransform r, Vector2 position) {
            r.anchoredPosition = position;
            return r;
        }
        
        /// <summary>
        /// Conveniently set recttransform anchored position.
        /// </summary>
        /// <param name="position">Anchored position to set.</param>
        public static RectTransform SetAnchoredPosition(this RectTransform r, float x, float y) {
            r.anchoredPosition = new Vector2(x, y);
            return r;
        }
        
        /// <summary>
        /// Conveniently set recttransform anchored position X.
        /// </summary>
        /// <param name="x">X anchored position to set.</param>
        public static RectTransform SetAnchoredPositionX(this RectTransform r, float x) {
            r.anchoredPosition = new Vector2(x, r.anchoredPosition.y);
            return r;
        }
        
        /// <summary>
        /// Conveniently set recttransform anchored position Y.
        /// </summary>
        /// <param name="y">Y anchored position to set.</param>
        public static RectTransform SetAnchoredPositionY(this RectTransform r, float y) {
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, y);
            return r;
        }
        
        /// <summary>
        /// Conveniently set recttransform offsets.
        /// </summary>
        public static RectTransform SetOffset(this RectTransform r, Vector2 offsetMin, Vector2 offsetMax) {
            r.offsetMin = offsetMin;
            r.offsetMax = offsetMax;
            return r;
        }
        
        /// <summary>
        /// Conveniently set recttransform offsets.
        /// </summary>
        public static RectTransform SetOffsetMin(this RectTransform r, Vector2 offsetMin) {
            r.offsetMin = offsetMin;
            return r;
        }
        
        /// <summary>
        /// Conveniently set recttransform offsets.
        /// </summary>
        public static RectTransform SetOffsetMinX(this RectTransform r, float offsetMinX) {
            r.offsetMin = new Vector2(offsetMinX, r.offsetMin.y);
            return r;
        }
        
        /// <summary>
        /// Conveniently set recttransform offsets.
        /// </summary>
        public static RectTransform SetOffsetMinY(this RectTransform r, float offsetMinY) {
            r.offsetMin = new Vector2(r.offsetMin.x, offsetMinY);
            return r;
        }
        
        /// <summary>
        /// Conveniently set recttransform offsets.
        /// </summary>
        public static RectTransform SetOffsetMax(this RectTransform r, Vector2 offsetMax) {
            r.offsetMax = offsetMax;
            return r;
        }
        
        /// <summary>
        /// Conveniently set recttransform offsets.
        /// </summary>
        public static RectTransform SetOffsetMaxX(this RectTransform r, float offsetMaxX) {
            r.offsetMax = new Vector2(offsetMaxX, r.offsetMax.y);
            return r;
        }
        
        /// <summary>
        /// Conveniently set recttransform offsets.
        /// </summary>
        public static RectTransform SetOffsetMaxY(this RectTransform r, float offsetMaxY) {
            r.offsetMax = new Vector2(r.offsetMax.x, offsetMaxY);
            return r;
        }

        /// <summary>
        /// Conveniently set anchor presets.
        /// </summary>
        /// <param name="anchorPreset">Anchor preset to set.</param>
        public static RectTransform SetAnchor(this RectTransform r, AnchorPreset anchorPreset) {
            switch (anchorPreset) {
                case AnchorPreset.TopLeft:
                    r.anchorMin = new Vector2(0f, 1f);
                    r.anchorMax = new Vector2(0f, 1f);
                    break;
                case AnchorPreset.TopCenter:
                    r.anchorMin = new Vector2(0.5f, 1f);
                    r.anchorMax = new Vector2(0.5f, 1f);
                    break;
                case AnchorPreset.TopRight:
                    r.anchorMin = new Vector2(1f, 1f);
                    r.anchorMax = new Vector2(1f, 1f);
                    break;
                case AnchorPreset.TopStretch:
                    r.anchorMin = new Vector2(0f, 1f);
                    r.anchorMax = new Vector2(1f, 1f);
                    break;
                case AnchorPreset.MiddleLeft:
                    r.anchorMin = new Vector2(0f, 0.5f);
                    r.anchorMax = new Vector2(0f, 0.5f);
                    break;
                case AnchorPreset.MiddleCenter:
                    r.anchorMin = new Vector2(0.5f, 0.5f);
                    r.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
                case AnchorPreset.MiddleRight:
                    r.anchorMin = new Vector2(1f, 0.5f);
                    r.anchorMax = new Vector2(1f, 0.5f);
                    break;
                case AnchorPreset.MiddleStretch:
                    r.anchorMin = new Vector2(0f, 0.5f);
                    r.anchorMax = new Vector2(1f, 0.5f);
                    break;
                case AnchorPreset.BottomLeft:
                    r.anchorMin = new Vector2(0f, 0f);
                    r.anchorMax = new Vector2(0f, 0f);
                    break;
                case AnchorPreset.BottomCenter:
                    r.anchorMin = new Vector2(0.5f, 0f);
                    r.anchorMax = new Vector2(0.5f, 0f);
                    break;
                case AnchorPreset.BottomRight:
                    r.anchorMin = new Vector2(1f, 0f);
                    r.anchorMax = new Vector2(1f, 0f);
                    break;
                case AnchorPreset.BottomStretch:
                    r.anchorMin = new Vector2(0f, 0f);
                    r.anchorMax = new Vector2(1f, 0f);
                    break;
                case AnchorPreset.StretchLeft:
                    r.anchorMin = new Vector2(0f, 0f);
                    r.anchorMax = new Vector2(0f, 1f);
                    break;
                case AnchorPreset.StretchCenter:
                    r.anchorMin = new Vector2(0.5f, 0f);
                    r.anchorMax = new Vector2(0.5f, 1f);
                    break;
                case AnchorPreset.StretchRight:
                    r.anchorMin = new Vector2(1f, 0f);
                    r.anchorMax = new Vector2(1f, 1f);
                    break;
                case AnchorPreset.StretchFull:
                    r.anchorMin = new Vector2(0f, 0f);
                    r.anchorMax = new Vector2(1f, 1f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(anchorPreset), anchorPreset, null);
            }
            return r;
        }

        public static Canvas GetParentCanvas(this GameObject gameObject) {
            var canvases = gameObject.GetComponentsInParent<Canvas>();
            if (canvases.Length <= 0) return null;

            return canvases[0];
        }

        
        #region Button
        public static Button AddButton(this RectTransform r) {
            return r.gameObject.AddComponent<Button>();
        }
        
        public static Button AddButton(this UIBehaviour u) {
            return u.gameObject.AddComponent<Button>();
        }
        

        public static Button ClearOnClick(this Button b) {
            b.onClick.RemoveAllListeners();
            return b;
        }

        public static Button AddOnClick(this Button b, UnityAction onClick, bool clear = false) {
            if (clear)
                b.onClick.RemoveAllListeners();
            b.onClick.AddListener(onClick);
            return b;
        }
        #endregion
        

        #region Image
        public static Image AddImage(this RectTransform r) {
            return r.gameObject.AddComponent<Image>();
        }
        
        public static Image AddImage(this Button b) {
            return b.gameObject.AddComponent<Image>();
        }
        
        public static Image SetColor(this Image i, Color color) {
            i.color = color;
            return i;
        }
        
        public static Image SetSprite(this Image i, Sprite sprite) {
            i.sprite = sprite;
            return i;
        }
        
        public static Image SetPreserveAspect(this Image i, bool preserveAspect) {
            i.preserveAspect = preserveAspect;
            return i;
        }

        public static Image SetRaycastTarget(this Image i, bool raycastTarget) {
            i.raycastTarget = raycastTarget;
            return i;
        }
        #endregion
        

        #region ScrollRect
        public static RectTransform GetRectTransform(this UIBehaviour uiBehaviour) {
            return uiBehaviour.GetComponent<RectTransform>();
        }

        public static ScrollRect SetHorizontal(this ScrollRect s, bool horizontal) {
            s.horizontal = horizontal;
            return s;
        }
        
        public static ScrollRect SetVertical(this ScrollRect s, bool vertical) {
            s.vertical = vertical;
            return s;
        }
        
        public static ScrollRect AddScrollRect(this RectTransform r) {
            return r.gameObject.AddComponent<ScrollRect>();
        }
        
        public static ScrollRect AddMask(this ScrollRect r) {
            r.gameObject.AddComponent<Mask>();
            return r;
        }
        
        public static ScrollRect AddImage(this ScrollRect r, Color c = default(Color)) {
            r.gameObject.AddComponent<Image>().color = c;
            return r;
        }
        #endregion
        

        #region Text
        public static Text AddText(this RectTransform r) {
            return r.gameObject.AddComponent<Text>();
        }

        public static Text SetText(this Text t, string text) {
            t.text = text;
            return t;
        }

        public static Text SetFont(this Text t, Font font) {
            t.font = font;
            return t;
        }

        public static Text SetColor(this Text t, Color color) {
            t.color = color;
            return t;
        }

        public static Text SetFontSize(this Text t, int fontSize) {
            t.fontSize = fontSize;
            return t;
        }
        
        public static Text SetFontSize(this Text t, float fontSize) {
            t.fontSize = UnityEngine.Mathf.FloorToInt(fontSize);
            return t;
        }

        public static Text SetFontStyle(this Text t, FontStyle fontStyle) {
            t.fontStyle = fontStyle;
            return t;
        }

        public static Text SetAlignment(this Text t, TextAnchor alignment) {
            t.alignment = alignment;
            return t;
        }
        #endregion
        
        
        #region GridLayout
        public static GridLayoutGroup AddGridLayoutGroup(this RectTransform r) {
            return r.gameObject.AddComponent<GridLayoutGroup>();
        }

        public static GridLayoutGroup SetContraint(this GridLayoutGroup g, GridLayoutGroup.Constraint constraint, int constraintCount) {
            g.constraint = constraint;
            g.constraintCount = constraintCount;
            return g;
        }
        
        public static GridLayoutGroup SetPadding(this GridLayoutGroup g, int left, int right, int top, int bottom) {
            g.padding = new RectOffset(left, right, top, bottom);
            return g;
        }
        
        public static GridLayoutGroup SetCellSize(this GridLayoutGroup g, float x, float y) {
            g.cellSize = new Vector2(x, y);
            return g;
        }
        
        public static GridLayoutGroup SetSpacing(this GridLayoutGroup g, float x, float y) {
            g.spacing = new Vector2(x, y);
            return g;
        }
        
        public static GridLayoutGroup SetChildAlignment(this GridLayoutGroup g, TextAnchor childAlignment) {
            g.childAlignment = childAlignment;
            return g;
        }
        #endregion
        
        
        #region HorizontalLayout
        public static HorizontalLayoutGroup AddHorizontalLayoutGroup(this RectTransform r) {
            return r.gameObject.AddComponent<HorizontalLayoutGroup>();
        }
        
        public static HorizontalLayoutGroup SetPadding(this HorizontalLayoutGroup g, int left, int right, int top, int bottom) {
            g.padding = new RectOffset(left, right, top, bottom);
            return g;
        }
        
        public static HorizontalLayoutGroup SetPadding(this HorizontalLayoutGroup g, int v) {
            g.padding = new RectOffset(v, v, v, v);
            return g;
        }
        
        public static HorizontalLayoutGroup SetSpacing(this HorizontalLayoutGroup g, float x) {
            g.spacing = x;
            return g;
        }
        
        public static HorizontalLayoutGroup SetChildAlignment(this HorizontalLayoutGroup g, TextAnchor childAlignment) {
            g.childAlignment = childAlignment;
            return g;
        }
        #endregion
        
        
        #region LayoutElement
        public static LayoutElement AddLayoutElement(this UIBehaviour r) {
            return r.gameObject.AddComponent<LayoutElement>();
        }
        
        public static LayoutElement SetMinWidth(this LayoutElement l, float x) {
            l.minWidth = x;
            return l;
        }

        public static LayoutElement SetMinSize(this LayoutElement l, float x, float y) {
            l.minHeight = y;
            l.minWidth = x;
            return l;
        }
        #endregion
        

        #region InputField
        public static InputField AddInputField(this UIBehaviour r) {
            return r.gameObject.AddComponent<InputField>();
        }
        
        public static InputField AddInputField(this RectTransform r) {
            return r.gameObject.AddComponent<InputField>();
        }
        
        public static InputField SetTransition(this InputField i, Selectable.Transition t) {
            i.transition = t;
            return i;
        }

        public static InputField SetContentType(this InputField i, InputField.ContentType t) {
            i.contentType = t;
            return i;
        }

        public static InputField SetKeyboardType(this InputField i, TouchScreenKeyboardType type) {
            i.keyboardType = type;
            return i;
        }

        public static InputField AddImage(this InputField i) {
            i.gameObject.AddComponent<Image>();
            return i;
        }
        
        public static InputField SetEvent(this InputField i, UnityAction<string> onValueChanged) {
            i.onValueChanged.RemoveAllListeners();
            i.onValueChanged.AddListener(onValueChanged);
            return i;
        }

        public static Text AddText(this InputField i, string defaultText) {
            var text = i.AddChild("Text").SetAnchor(AnchorPreset.StretchFull).SetAnchoredPositionX(5).SetSize(-10, 0);
            i.textComponent = text.gameObject.AddComponent<Text>();
            i.textComponent.text = defaultText;
            return i.textComponent;
        }
        #endregion


        #region Dropdown

        public static Dropdown AddDropdown(this RectTransform r, float fontSize, float dropDownAreaSize, float itemSize) {
            var d = r.gameObject.AddComponent<Dropdown>();
            var font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            
            d.captionText = r.AddChild("Label").SetAnchor(AnchorPreset.StretchFull).SetAnchoredPosition(5, 0).SetSize(-10, 0).AddText().SetColor(Color.black).SetFont(font)
                .SetFontSize(fontSize).SetAlignment(TextAnchor.MiddleLeft);
            //r.AddChild("Arrow").AddImage().sprite =
            var template = r.AddChild("Template").SetAnchor(AnchorPreset.BottomStretch).SetPivot(0.5f, 1f).SetAnchoredPosition(0, 0).SetSize(0, dropDownAreaSize);
            template.AddImage();
            var scrollRect = template.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            var viewport = template.AddChild("ViewPort").SetAnchor(AnchorPreset.StretchFull).SetAnchoredPosition(10, 0).SetSize(0, 0);
            scrollRect.viewport = viewport;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = viewport.AddChild("Content").SetAnchor(AnchorPreset.TopStretch).SetAnchoredPosition(0, 0).SetPivot(0.5f, 1f).SetSize(0, itemSize);
            scrollRect.content = content;
            var item = content.AddChild("Item").SetAnchor(AnchorPreset.MiddleStretch).SetSize(0, itemSize);
            item.gameObject.AddComponent<Toggle>();
            var label = item.AddChild("Item Label").SetAnchor(AnchorPreset.StretchFull).SetAnchoredPosition(5, 0).SetSize(-10, 0).AddText().SetColor(Color.black).SetFont(font)
                .SetFontSize(fontSize).SetAlignment(TextAnchor.MiddleLeft);
            
            d.template = template;
            d.itemText = label;
            template.gameObject.SetActive(false);
            return d;
        }
        
//        
//        public static Sprite GetBoundSpriteInChildren(this RectTransform r) {
//            
//        }
//        
//        private static Rect GetBoundSpriteInChildrenInternal(this RectTransform r, int depth) {
//            if (depth >= 20) {
//                Debug.LogError("ENDLESS RECURSIVE HERE!!!");
//                return new Rect();
//            }
//            
//            if (r == null) return new Rect();
//            
//            if (r.childCount <= 0)
//                return r.rect;
//            
//            var rect = r.rect;
//            for (var i = 0; i < r.childCount; i++) {
//                var childRectTransform = r.GetChild(i).GetComponent<RectTransform>();
//                if (childRectTransform != null)
//                    rect = rect.GetBound(childRectTransform.GetBoundRectIncludeChildren(depth + 1));
//            }
//
//            return rect;
//        }

/// <summary>
        /// Conveniently set rect transform size.
        /// </summary>
        /// <param name="sizeX">Size X to set.</param>
        public static RectTransform SetSizeX(this RectTransform r, float sizeX) {
            r.sizeDelta = new Vector2(sizeX, r.sizeDelta.y);
            return r;
        }
        
        /* Remove this because it causes too much GC alloc.
        /// <summary>
        /// Get rect transform 's rect independent from pivot.
        /// </summary>
        public static Rect GetScreenRect(this RectTransform r) {
            var corners = new Vector3[4];
            r.GetWorldCorners(corners);
            return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }*/
        
        /// <summary>
        /// Conveniently set recttransform size.
        /// </summary>
        public static RectTransform SetSize(this RectTransform r, float size) {
            r.sizeDelta = new Vector2(size, size);
            return r;
        }
        
        /// <summary>
        /// Conveniently set rect transform size.
        /// </summary>
        public static RectTransform SetSizeY(this RectTransform r, float sizeY) {
            r.sizeDelta = new Vector2(r.sizeDelta.x, sizeY);
            return r;
        }

        public static RectTransform SetAnchoredDistanceTop(this RectTransform r, float distanceTop) {
            return r.SetSizeY(-distanceTop).SetAnchoredPositionY(distanceTop * -0.5f);
        }
        
        public static RectTransform SetAnchoredDistanceRight(this RectTransform r, float distanceTop) {
            return r.SetSizeX(-distanceTop).SetAnchoredPositionX(-distanceTop * r.pivot.x);
        }
        
        public static RectTransform SetAnchoredDistanceLeft(this RectTransform r, float distanceTop) {
            return r.SetSizeX(-distanceTop).SetAnchoredPositionX(distanceTop * (1 - r.pivot.x));
        }
        
        public static RectTransform SetAnchoredDistanceHorizontal(this RectTransform r, float distanceLeft, float distanceRight) {
            return r.SetSizeX(-distanceLeft - distanceRight).SetAnchoredPositionX((distanceLeft - distanceRight) / 2);
        }
        public static RectTransform SetAnchoredDistanceHorizontal(this RectTransform r, float distanceLeftAndRight) {
            return r.SetSizeX(distanceLeftAndRight * -2);
        }
        
        public static RectTransform SetAnchoredDistanceVertical(this RectTransform r, float distanceTopAndBottom) {
            return r.SetSizeY(distanceTopAndBottom * -2);
        }
        #endregion
    }
}