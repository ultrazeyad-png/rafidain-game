using UnityEngine;
using UnityEngine.EventSystems;

namespace Rafidain
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform knob;
        public float radius = 120f;
        RectTransform rt;

        void Awake() => rt = GetComponent<RectTransform>();

        public void OnPointerDown(PointerEventData e) => OnDrag(e);

        public void OnDrag(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local);
            Vector2 v = Vector2.ClampMagnitude(local, radius);
            if (knob != null) knob.anchoredPosition = v;
            if (PlayerInputReader.Instance != null) PlayerInputReader.Instance.SetMobileMove(v / radius);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (PlayerInputReader.Instance != null) PlayerInputReader.Instance.SetMobileMove(Vector2.zero);
        }
    }
}
