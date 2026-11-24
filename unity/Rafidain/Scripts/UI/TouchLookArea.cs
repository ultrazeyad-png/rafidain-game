using UnityEngine;
using UnityEngine.EventSystems;

namespace Rafidain
{
    public class TouchLookArea : MonoBehaviour, IDragHandler
    {
        public void OnDrag(PointerEventData e)
        {
            if (PlayerInputReader.Instance != null) PlayerInputReader.Instance.AddMobileLook(e.delta);
        }
    }
}
