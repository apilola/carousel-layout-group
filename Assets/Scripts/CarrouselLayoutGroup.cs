using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MovementType = UnityEngine.UI.ScrollRect.MovementType;
public class CarrouselLayoutGroup : UIBehaviour, ILayoutGroup, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    float m_Velocity;
    bool m_IsDragging;
    bool m_DidScroll;
    float m_SnapVelocity;
    float m_Length;

    [field: SerializeField] public float Value { get; set; }

    [field: Header("Presentation Settings")]
    [field: SerializeField] public float Spacing { get; set; }
    [field: SerializeField, Min(.25f)] public float RadiusScale { get; set; } = 1f;
    [field: SerializeField] public bool Invert { get; set; }
    [field: SerializeField] public bool ReverseArangement { get; set; }
    [field: SerializeField, Range(0, 1)] public float BlendArea { get; set; } = 1;
    [field: SerializeField] public bool Alpha { get; set; } = false;
    [field: SerializeField] public bool ControlChildRotation { get; set; } = true;

    [field: Header("Scroll Settings")]
    [field: SerializeField] public MovementType MovementType { get; set; } = MovementType.Elastic;
    [field: SerializeField] public float Elasticity { get; set; } = .1f;
    [field: SerializeField] public bool Inertia { get; set; } = true;
    [field: SerializeField, Min(0)] public float DecelerationRate { get; set; } = .135f;
    [field: SerializeField, Min(0)] public float ScrollSensativity { get; set; } = 5;
    [field: SerializeField] public bool SnapToNearest { get; set; } = true;
    [field: SerializeField] public float SnapElasticity { get; set; } = .1f;


    [System.NonSerialized]
    private ChildItem m_NearestChild;
    protected ChildItem NearestChild
    {
        get
        {
            return m_NearestChild;
        }
    }

    private Transform FocusedItem
    {
        get
        {
            ChildItem child = m_NearestChild;
            if(child == null)
            {
                return null;
            }

            return child.transform;
        }
        set{

            for(var i = 0; i < childData.Count; i++)
            {
                var child = childData[i];
                if(child.transform == value)
                {
                    m_NearestChild = child;
                    break;
                }
            }
        }
    }

    [System.NonSerialized]
    private bool m_GizmoInit;

    public float Radius => this.rectTransform.rect.height * RadiusScale *.5f;
#if UNITY_EDITOR
    [field: Header("Editor Settings")]
    [field: SerializeField] public bool GizmoAlpha { get; set; }
#endif

    [System.NonSerialized] private RectTransform m_Rect;
    protected RectTransform rectTransform
    {
        get
        {
            if (m_Rect == null)
                m_Rect = GetComponent<RectTransform>();
            return m_Rect;
        }
    }

    [System.NonSerialized] private List<ChildItem> m_ChildData = new List<ChildItem>();
    protected List<ChildItem> childData { get { return m_ChildData; } }

    [System.Serializable]   
    public class ChildItem
    {
        public RectTransform transform;
        public float theta;
        public int index;
    }

    private void OnDrawGizmosSelected()
    {
        if(!m_GizmoInit)
        {
            RefreshChildren();
            m_GizmoInit = true;
        }
        var rect = this.rectTransform.rect;
        var spacing = GetTheta(Spacing);
        var ltwMat = transform.localToWorldMatrix;
        var radius = Radius;
        var trsMat = Matrix4x4.TRS(new Vector3(0, 0, radius), Quaternion.identity, Vector3.one);
        float direction = (Invert) ? 1f : -1f;
        float theta = -Value;
        Vector2 size = new Vector2(rect.width, rect.width);
        if (size.y == 0)
            size.y = 1f;
        var limit = -Value + Mathf.PI * 2;
        for (int i = 0; i < m_ChildData.Count || theta < limit; i++)
        {
            ChildItem child = (i < m_ChildData.Count) ? m_ChildData[i] : null;
            if(child != null)
            {
                size = child.transform.rect.size;
                size *= child.transform.localScale;
                if (size.y == 0)
                    size.y = 1f;
            }
            float lastSize = GetTheta(size.y);

            if (i != 0)
            {
                theta += lastSize * .5f;
            }

            var rMat = ConstructAngleAxis(Mathf.PI + theta * direction, Vector3.right);
            var mat = ltwMat * Matrix4x4.Translate(new Vector3(0, 0, radius)) * rMat * trsMat; 
            if(i < m_ChildData.Count)
            {
                UpdateChildData(child, theta);
            }
            //theta += lastSize;
            //theta += lastSize *.5f;

            theta += lastSize * .5f + spacing;

            if (theta < limit)
            {
                var alpha = GetAlpha(theta);
                DrawFace(mat, size, alpha);
            }
        }        
    }

    private ChildItem UpdateChildData(ChildItem child, float theta)
    {
        child.theta = m_Length = theta + Value;
        RefreshNearestChild(child);
        return child;
    }

    float GetTheta(float height)
    {
        return Mathf.Asin((height / 2f) / Radius) * 2;
    }

    float GetAlpha(float theta)
    {
        return Mathf.SmoothStep(0,1, Mathf.InverseLerp(Mathf.PI * BlendArea, 0, Mathf.Abs(theta)));
    }

    Matrix4x4 ConstructAngleAxis(float theta, Vector3 axis)
    {
        var half = theta / 2;
        var sinHalf = Mathf.Sin(half);
        return Matrix4x4.Rotate(new Quaternion(axis.x * sinHalf, axis.y * sinHalf, axis.z * sinHalf, Mathf.Cos(half)));
    }

    void RefreshNearestChild(ChildItem childItem)
    {
        if (childItem == null)
        {
            return;
        }

        if (m_NearestChild == null || m_NearestChild.transform == null)
        {
            m_NearestChild = childItem;
            return;
        }

        if(Mathf.Abs(Value - childItem.theta) < Mathf.Abs(Value - m_NearestChild.theta) )
        {
            m_NearestChild = childItem;
        }
    }

    public void DrawFace(Matrix4x4 mat, Vector3 scale, float alpha)
    {
        Color oCol = Gizmos.color;
        var col = oCol;
        col.r = alpha;
        col.g = alpha;
        col.b = alpha;
#if UNITY_EDITOR
        if (GizmoAlpha)
        {
            col.a = alpha;
        }
#endif
        Gizmos.color = col;
        var lScale = mat.lossyScale;
        lScale.Scale(scale);
        var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        Gizmos.DrawWireMesh(cube, mat.GetPosition(), mat.rotation, lScale);
        Gizmos.color = oCol;
    }

    void RefreshChildren()
    {
        m_ChildData.Clear();
        if(ReverseArangement)
        {
            for (int i = rectTransform.childCount - 1; i >= 0; i--)
            {
                EvaluateChild(rectTransform.GetChild(i) as RectTransform);
            }
        }
        else
        {
            for (int i = 0; i < rectTransform.childCount; i++)
            {
                EvaluateChild(rectTransform.GetChild(i) as RectTransform);
            }
        }

        void EvaluateChild(RectTransform rect)
        {
            if (rect == null)
                return;
            ILayoutIgnorer ignorer = rect.GetComponent(typeof(ILayoutIgnorer)) as ILayoutIgnorer;
            if (rect.gameObject.activeInHierarchy && !(ignorer != null && ignorer.ignoreLayout))
            {
                m_ChildData.Add(new ChildItem()
                {
                    transform = rect,
                    index = m_ChildData.Count                    
                });
            }
        }
    }
    public void SetLayoutHorizontal()
    {
        RefreshChildren();

    }

    public void SetLayoutVertical()
    {
        RefreshChildren();
        ArrangeChildren();
    }

    private void ArrangeChildren()
    {
        var spacing = GetTheta(Spacing);
        var ltwMat = transform.localToWorldMatrix;
        var radius = Radius;
        var trsMat = Matrix4x4.TRS(new Vector3(0, 0, -radius), Quaternion.identity, Vector3.one);
        float direction = (Invert) ? 1f : -1f;
        float theta = -Value;
        float lastSize = 0;
        for (int i = 0; i < m_ChildData.Count; i++)
        {
            ChildItem child = m_ChildData[i];

            var size = child.transform.rect.size * child.transform.localScale;

            lastSize = GetTheta(size.y);

            if (i != 0)
            {
                theta += lastSize * .5f;
            }

            var rMat = ConstructAngleAxis(theta * direction, Vector3.right);
            var mat = ltwMat * Matrix4x4.Translate(new Vector3(0, 0, radius)) * rMat * trsMat;

            UpdateChildData(child,theta);
            child.transform.position = mat.GetPosition();
            if(ControlChildRotation)
            {
                child.transform.rotation = mat.rotation;
            }

            if (Alpha)
            {
                var canvasGroup = child.transform.GetComponent<CanvasGroup>();
                if (canvasGroup)
                {
                    var alpha = GetAlpha(theta);
                    canvasGroup.alpha = alpha;
                }
            }

            var delta = (lastSize *.5f + spacing);

            theta += delta;
        }
    }

    void LateUpdate()
    {
        float offset = CalculateOffset(0);
        if (!m_IsDragging && (offset != 0|| m_Velocity != 0))
        {
            float value = Value;
            var deltaTime = Time.deltaTime;
            if (MovementType == MovementType.Elastic && offset != 0)
            {
                float speed = m_Velocity;
                value = Mathf.SmoothDamp(Value, Value + offset, ref speed, Elasticity, Mathf.Infinity, deltaTime);
                m_Velocity = speed;
            }
            else if(Inertia)
            {
                m_Velocity *= Mathf.Pow(DecelerationRate, deltaTime);
                value += m_Velocity * deltaTime;
                SetDirty();
            }
            else
            {
                m_Velocity = 0;
            }

            if (Mathf.Abs(m_Velocity) < .1f)
            {
                m_Velocity = 0;
            }
            else if (m_Velocity != 0)
            {
                if (MovementType == MovementType.Clamped)
                {
                    offset = CalculateOffset(value - Value);
                    value += offset;
                }

                if(value != Value)
                {
                    Value = value;
                    SetDirty();
                }
            }
        }

        if(SnapToNearest && m_Velocity == 0 && !m_DidScroll)
        {
            offset = CalculateOffsetFromNearestChild();
            if (offset != 0)
            {
                Value = Mathf.SmoothDamp(Value, Value + offset, ref m_SnapVelocity, SnapElasticity, Mathf.Infinity, Time.deltaTime);
                SetDirty();
            }
        }

        if (m_DidScroll)
        {
            m_DidScroll = false;
        }
    }

    private float CalculateOffsetFromNearestChild()
    {
        float offset = 0;
        if (m_NearestChild == null || m_NearestChild.transform == null)
        {
            return offset;
        }

        return m_NearestChild.theta - Value;
    }
    private float CalculateOffset(float delta)
    {
        float offset = 0;
        if (MovementType == MovementType.Unrestricted)
            return offset;

        float min = 0;
        float max = m_Length;
        var value = Value + delta;

        //if our value is less than 
        if (value < min)
        {
            offset = min - value;
        }
        else if (value > max)
        {
            offset = max - value;
        }

        return offset;
    }
#if UNITY_EDITOR
    protected override void OnValidate()
    {
        SetDirty();
    }

#endif

    private void FocusItem(ChildItem child)
    {
        if (child == null)
            return;
        m_NearestChild = child;
    }

    [ContextMenu("FocusFirstItem")]
    public void FocusFirstItem()
    {
        var child = m_ChildData[0];
        if (child == null)
        {
            return;
        }
        FocusItem(child);
    }

    protected void SetDirty()
    {
        if (!IsActive())
            return;

        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

    void IScrollHandler.OnScroll(PointerEventData eventData)
    {
        if(eventData.scrollDelta.y != 0)
        {
            var speed = GetTheta(-eventData.scrollDelta.y * ScrollSensativity);
            Value += speed;
            m_Velocity = 0;
            m_DidScroll = true;
            SetDirty();
        }
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        m_IsDragging = true;
        HandleDrag(eventData.delta.y);
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        HandleDrag(eventData.delta.y);
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        HandleDrag(eventData.delta.y);
        m_IsDragging = false;
    }

    void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData)
    {
        m_Velocity = 0;
    }

    void HandleDrag(float value)
    {
        var speed = GetTheta(value);
        Value += speed;
        m_Velocity = speed / Time.deltaTime;
        SetDirty();
    }
}
