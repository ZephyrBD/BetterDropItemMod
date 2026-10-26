using RoR2;
using UnityEngine;

namespace BetterDropItem
{
    public class MarkNonRecyclableComponent : MonoBehaviour
    {
        public void Start()
        {
            GenericPickupController gpc = base.GetComponent<GenericPickupController>();
            if (gpc)
            {
                gpc.Duplicated = true;
                gpc.Recycled = true;
                Destroy(this);
            }
        }
    }
}
