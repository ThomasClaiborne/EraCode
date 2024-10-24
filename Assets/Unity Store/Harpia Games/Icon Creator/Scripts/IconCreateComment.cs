using UnityEngine;

namespace Harpia.IconCreator
{
    public class IconCreateComment : MonoBehaviour
    {
#if UNITY_EDITOR

        //Safe to delete this script

        //This script is used to add a comment to objects

        [TextArea(3, 10)]
        public string comment;

#endif
    }
}