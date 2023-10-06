using UnityEngine;

namespace HSR.MotionCapture.Tests
{
    [RequireComponent(typeof(LineRenderer))]
    public class DynamicLine : MonoBehaviour
    {
        [SerializeField] private Transform m_Point1;
        [SerializeField] private Transform m_Point2;
        private LineRenderer m_Renderer;

        private void Start()
        {
            m_Renderer = GetComponent<LineRenderer>();
        }

        private void Update()
        {
            m_Renderer.SetPosition(0, m_Point1.position);
            m_Renderer.SetPosition(1, m_Point2.position);
        }
    }
}
