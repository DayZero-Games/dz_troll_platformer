using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DZ.Features
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class PixelPerfectBorderOverlay : MonoBehaviour
    {
        private const int Left = 0;
        private const int Right = 1;
        private const int Bottom = 2;
        private const int Top = 3;

        [SerializeField] private PixelPerfectCamera pixelPerfectCamera;
        [SerializeField] private Sprite borderSprite;
        [SerializeField] private Color borderColor = new Color32(255, 255, 255, 255);
        [SerializeField, Range(-32768, 32767)] private int sortingOrder = -100;

        private readonly Image[] _bars = new Image[4];
        private Vector2Int _lastScreenSize;

        private void Awake()
        {
            ConfigureCanvas();
            CreateBars();
            RefreshLayout();
        }

        private void OnEnable()
        {
            _lastScreenSize = Vector2Int.zero;
        }

        private void Update()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize == _lastScreenSize) return;

            RefreshLayout();
        }

        private void ConfigureCanvas()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            var canvasScaler = GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        }

        private void CreateBars()
        {
            CreateBar(Left, "Left Border");
            CreateBar(Right, "Right Border");
            CreateBar(Bottom, "Bottom Border");
            CreateBar(Top, "Top Border");
        }

        private void CreateBar(int index, string barName)
        {
            var bar = new GameObject(barName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bar.transform.SetParent(transform, false);

            var image = bar.GetComponent<Image>();
            image.raycastTarget = false;
            _bars[index] = image;
        }

        private void RefreshLayout()
        {
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            if (pixelPerfectCamera.cropFrame != PixelPerfectCamera.CropFrame.StretchFill)
            {
                SetBar(Left, Vector2.zero, Vector2.zero, false);
                SetBar(Right, Vector2.zero, Vector2.zero, false);
                SetBar(Bottom, Vector2.zero, Vector2.zero, false);
                SetBar(Top, Vector2.zero, Vector2.zero, false);
                return;
            }

            var referenceAspect = (float)pixelPerfectCamera.refResolutionX / pixelPerfectCamera.refResolutionY;
            var screenAspect = (float)Screen.width / Screen.height;

            if (screenAspect > referenceAspect)
            {
                var contentWidth = referenceAspect / screenAspect;
                var borderWidth = (1f - contentWidth) * 0.5f;

                SetBar(Left, Vector2.zero, new Vector2(borderWidth, 1f), true);
                SetBar(Right, new Vector2(1f - borderWidth, 0f), Vector2.one, true);
                SetBar(Bottom, Vector2.zero, Vector2.zero, false);
                SetBar(Top, Vector2.zero, Vector2.zero, false);
                return;
            }

            var contentHeight = screenAspect / referenceAspect;
            var borderHeight = (1f - contentHeight) * 0.5f;

            SetBar(Left, Vector2.zero, Vector2.zero, false);
            SetBar(Right, Vector2.zero, Vector2.zero, false);
            SetBar(Bottom, Vector2.zero, new Vector2(1f, borderHeight), true);
            SetBar(Top, new Vector2(0f, 1f - borderHeight), Vector2.one, true);
        }

        private void SetBar(int index, Vector2 anchorMin, Vector2 anchorMax, bool isVisible)
        {
            var image = _bars[index];
            image.sprite = borderSprite;
            image.color = borderColor;

            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            image.gameObject.SetActive(isVisible);
        }
    }
}