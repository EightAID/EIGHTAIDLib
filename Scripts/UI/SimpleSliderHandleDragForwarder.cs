using UnityEngine;
using UnityEngine.EventSystems;

namespace EightAID.EIGHTAIDLib.UI
{
    [DisallowMultipleComponent]
    public sealed class SimpleSliderHandleDragForwarder : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private SimpleSlider slider;

        public void SetSlider(SimpleSlider target)
        {
            slider = target;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ResolveSlider();
            slider?.OnPointerDown(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ResolveSlider();
            slider?.OnPointerDown(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            ResolveSlider();
            slider?.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }

        private void ResolveSlider()
        {
            if (slider == null)
            {
                slider = GetComponentInParent<SimpleSlider>();
            }
        }
    }
}
