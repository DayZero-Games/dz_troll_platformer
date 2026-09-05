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
        private PixelPerfectCamera.CropFrame _lastCropFrame;

        private void Awake()
        {
            ConfigureCanvas();
            CreateBars();
            RefreshLayout();
        }

private void OnEnable()
        {
            _lastScreenSize = Vector2Int.zero;
            _lastCropFrame = (PixelPerfectCamera.CropFrame)(-1);
        }

private void Update()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var cropFrame = pixelPerfectCamera.cropFrame;
            if (screenSize == _lastScreenSize && cropFrame == _lastCropFrame) return;

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
            _lastCropFrame = pixelPerfectCamera.cropFrame;

            var referenceAspect = (float)pixelPerfectCamera.refResolutionX / pixelPerfectCamera.refResolutionY;
            var screenAspect = (float)Screen.width / Screen.height;

            switch (_lastCropFrame)
            {
                case PixelPerfectCamera.CropFrame.None:
                    SetBorders(1f, 1f);
                    return;

                case PixelPerfectCamera.CropFrame.Pillarbox:
                    SetBorders(Mathf.Min(1f, referenceAspect / screenAspect), 1f);
                    return;

                case PixelPerfectCamera.CropFrame.Letterbox:
                    SetBorders(1f, Mathf.Min(1f, screenAspect / referenceAspect));
                    return;

                case PixelPerfectCamera.CropFrame.Windowbox:
                    var pixelRatio = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(
                        (float)Screen.width / pixelPerfectCamera.refResolutionX,
                        (float)Screen.height / pixelPerfectCamera.refResolutionY)));
                    SetBorders(
                        (float)(pixelPerfectCamera.refResolutionX * pixelRatio) / Screen.width,
                        (float)(pixelPerfectCamera.refResolutionY * pixelRatio) / Screen.height);
                    return;

                case PixelPerfectCamera.CropFrame.StretchFill:
                    if (screenAspect > referenceAspect)
                    {
                        SetBorders(referenceAspect / screenAspect, 1f);
                        return;
                    }

                    SetBorders(1f, screenAspect / referenceAspect);
                    return;
            }
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
    

private void SetBorders(float contentWidth, float contentHeight)
        {
            contentWidth = Mathf.Clamp01(contentWidth);
            contentHeight = Mathf.Clamp01(contentHeight);

            var horizontalBorder = (1f - contentWidth) * 0.5f;
            var verticalBorder = (1f - contentHeight) * 0.5f;

            SetBar(Left, Vector2.zero, new Vector2(horizontalBorder, 1f), horizontalBorder > 0f);
            SetBar(Right, new Vector2(1f - horizontalBorder, 0f), Vector2.one, horizontalBorder > 0f);
            SetBar(Bottom, new Vector2(horizontalBorder, 0f), new Vector2(1f - horizontalBorder, verticalBorder), verticalBorder > 0f);
            SetBar(Top, new Vector2(horizontalBorder, 1f - verticalBorder), new Vector2(1f - horizontalBorder, 1f), verticalBorder > 0f);
        }
}
}