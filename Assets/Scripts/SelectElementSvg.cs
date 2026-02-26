using System;
using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static Unity.VectorGraphics.SVGParser;
using TMPro;

public class SelectElementSvg : MonoBehaviour
{
    [Header("SVG Settings")]
    [SerializeField]
    private TextAsset _svgFile;
    [SerializeField]
    private SVGImage _svgImage;
    [SerializeField]
    private RectTransform _svgRootPosition;

    [Space(10)]
    [Header("Initial Focus")]        
    [SerializeField]
    private string _nameId = "layer16";
    [SerializeField]
    private Color _color = Color.red;
    [SerializeField, Range(1, 10f)]
    private float _scale = 10f;

    [Space(10)]
    [Header("Zoom & Pan Settings")]
    [SerializeField] 
    private float _zoomSensitivity = 0.1f;
    [SerializeField] 
    private float _minScale = 0.5f;
    [SerializeField] 
    private float _maxScale = 10f;
    [SerializeField] 
    private float _panSensitivity = 1f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference _panAction;
    [SerializeField] private InputActionReference _zoomAction;
    [SerializeField] private InputActionReference _dragStartAction;
    [SerializeField] private InputActionReference _dragEndAction;

    private Rect _svgSceneBounds;
    private Rect _groupBounds; //границы выбранной страны
    private Vector2 _centerInLocalSpace;

    private Vector2 _currentPanOffset = Vector2.zero;
    private float _currentScale = 1f;
    private bool _isDragging = false;

    private SceneInfo _sceneInfo;
    private Dictionary<string, SceneNode> _countryNodes = new();

    private SceneNode _currentlySelectedNode = null;
    private Color _defaultColor = Color.white;

    private void OnEnable()
    {
        EnableInput();
    }

    private void OnDisable()
    {
        DisableInput();
    }

    private void EnableInput()
    {
        if (_panAction != null) _panAction.action.Enable();
        if (_zoomAction != null) _zoomAction.action.Enable();
        if (_dragStartAction != null) _dragStartAction.action.Enable();
        if (_dragEndAction != null) _dragEndAction.action.Enable();

        if (_dragStartAction != null)
            _dragStartAction.action.performed += OnDragStart;
        if (_dragEndAction != null)
            _dragEndAction.action.performed += OnDragEnd;
    }

    private void DisableInput()
    {
        if (_dragStartAction != null)
            _dragStartAction.action.performed -= OnDragStart;
        if (_dragEndAction != null)
            _dragEndAction.action.performed -= OnDragEnd;

        if (_panAction != null) _panAction.action.Disable();
        if (_zoomAction != null) _zoomAction.action.Disable();
        if (_dragStartAction != null) _dragStartAction.action.Disable();
        if (_dragEndAction != null) _dragEndAction.action.Disable();
    }

    private void Start()
    {
        if (_svgFile == null || _svgImage == null || _svgRootPosition == null)
        {
            Debug.LogError("Assign SVG File, SVGImage and Root RectTransform!");
            return;
        }

        _sceneInfo = SVGParser.ImportSVG(new StringReader(_svgFile.text));
        // общий bounding box ВСЕГО SVG
        _svgSceneBounds = VectorUtils.SceneNodeBounds(_sceneInfo.Scene.Root);

        foreach (var kvp in _sceneInfo.NodeIDs)
        {
            _countryNodes[kvp.Key] = kvp.Value;
            Debug.Log($"Country ID: {kvp.Key}");
        }
                
        SelectCountry(_nameId, _scale);
        SelectCountry("USA", 5f);
    }

    public void SelectCountry(string _nameCountry, float scale)
    {
        

        if (_sceneInfo.NodeIDs.TryGetValue(_nameCountry, out var groupNode))
        {
            ChangeColorByGroup(groupNode, _color);
            _groupBounds = VectorUtils.SceneNodeBounds(groupNode);

            Vector2 normalized = GetNormalizedPosition(_groupBounds, _svgSceneBounds);

            _centerInLocalSpace = ConvertNormalizedToRectTransformPosition(normalized, _svgRootPosition);

            float targetScale = GetZoomScale(_groupBounds, scale);

            ZoomOnCountry(targetScale);

            ApplyTransform();
        }
        else
        {
            Debug.LogWarning($"Группа с id '{_nameCountry}' не найдена в SVG.");
        }

        GenerateSprite(_sceneInfo);
    }       

    private void Update()
    {
        HandleZoom();
        HandlePan();
    }

    private void HandleZoom()
    {
        if (_zoomAction == null) return;

        Vector2 scroll = _zoomAction.action.ReadValue<Vector2>();
        if (scroll.y != 0)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 localMousePos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _svgRootPosition.parent as RectTransform,
                mousePos,
                null,
                out localMousePos))
            {
                Vector2 worldMouseInSvgSpace = localMousePos - _currentPanOffset;
                Vector2 svgLocalMouse = worldMouseInSvgSpace / _currentScale;

                float zoomDelta = scroll.y * _zoomSensitivity;
                _currentScale += zoomDelta;
                _currentScale = Mathf.Clamp(_currentScale, _minScale, _maxScale);

                Vector2 newWorldMouseInSvgSpace = svgLocalMouse * _currentScale;
                _currentPanOffset = localMousePos - newWorldMouseInSvgSpace;
            }
        }
    }

    private void HandlePan()
    {
        if (!_isDragging) return;
        if (_panAction == null) return;

        Vector2 delta = _panAction.action.ReadValue<Vector2>();
        if (delta != Vector2.zero)
        {
            _currentPanOffset += delta * _panSensitivity;
        }
    }

    private void OnDragStart(InputAction.CallbackContext context)
    {
        _isDragging = true;
        Debug.Log("OnDragStart");
    }

    private void OnDragEnd(InputAction.CallbackContext context)
    {
        _isDragging = false;
        Debug.Log("OnDragEnd");
    }

    private void ApplyTransform()
    {
        _svgRootPosition.localScale = new Vector3(_currentScale, _currentScale, 1f);
        _svgRootPosition.anchoredPosition = _currentPanOffset;
    }

    private void LateUpdate()
    {
        ApplyTransform();
    }

    private void GenerateSprite(SceneInfo sceneInfo)
    {
        var tessOptions = new VectorUtils.TessellationOptions()
        {
            StepDistance = 500f,
            MaxCordDeviation = 0.5f,
            MaxTanAngleDeviation = 0.1f,
            SamplingStepSize = 0.01f
        };

        var sprite = VectorUtils.BuildSprite(
            VectorUtils.TessellateScene(sceneInfo.Scene, tessOptions),
            100f,
            VectorUtils.Alignment.Center, // центрирует спрайт
            Vector2.zero,
            128,
            true
        );

        _svgImage.sprite = sprite;
    }

    public void ChangeColorByGroup(SceneNode node, Color newColor)
    {
        if (node.Shapes != null)
        {
            foreach (var shape in node.Shapes)
            {
                if (shape.Fill is SolidFill fill)
                {
                    fill.Color = newColor;
                }
            }
        }

        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                ChangeColorByGroup(child, newColor);
            }
        }
    }

    private Vector2 GetNormalizedPosition(Rect groupBounds, Rect svgBounds)
    {
        // номализация от 0 до 1 относительно всего SVG
        // (0,0) = левый верхний угол SVG
        float ormalizedX = (groupBounds.center.x - svgBounds.xMin) / svgBounds.width;
        float ormalizedY = (groupBounds.center.y - svgBounds.yMin) / svgBounds.height;
        return new Vector2(ormalizedX, ormalizedY);
    }

    private Vector2 ConvertNormalizedToRectTransformPosition(Vector2 normalizedPos, RectTransform rectTransform)
    {
        Vector2 size = rectTransform.rect.size;
                
        // - normalizedPos: (0,0) = левый верх SVG, (1,1) = правый низ
        // - В UI: центр = (0,0), Y вверх
        float x = (normalizedPos.x - 0.5f) * size.x;      // от -width/2 до +width/2
        float y = (0.5f - normalizedPos.y) * size.y;

        return new Vector2(x, y);
    }

    private float GetZoomScale(Rect bounds, float scale = 500f)
    {
        float maxSide = Mathf.Max(bounds.width, bounds.height);
        if (maxSide <= 0) return 1f;
        return scale / maxSide;
    }

    private void ZoomOnCountry(float targetScale)
    {
        _currentScale = Mathf.Clamp(targetScale, _minScale, _maxScale);
        _currentPanOffset = -_centerInLocalSpace * _currentScale;
    }
}