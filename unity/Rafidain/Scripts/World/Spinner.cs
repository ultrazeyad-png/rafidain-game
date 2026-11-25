using UnityEngine;

namespace Rafidain
{
    /// <summary>Turns slowly around an axis (waterwheels, windmills...).</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 axis = Vector3.forward;
        public float degreesPerSecond = 10f;
        void Update() => transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
