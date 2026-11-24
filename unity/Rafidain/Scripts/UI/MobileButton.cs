using UnityEngine;
using UnityEngine.EventSystems;

namespace Rafidain
{
    public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public GameAction action;
        public bool holdToSprint;

        public void OnPointerDown(PointerEventData e)
        {
            var input = PlayerInputReader.Instance;
            if (input == null) return;
            if (holdToSprint) input.SetMobileSprint(true);
            else input.Press(action);
            transform.localScale = Vector3.one * 0.92f;
        }

        public void OnPointerUp(PointerEventData e)
        {
            var input = PlayerInputReader.Instance;
            if (input != null && holdToSprint) input.SetMobileSprint(false);
            transform.localScale = Vector3.one;
        }
    }
}
