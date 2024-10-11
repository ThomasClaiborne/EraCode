using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float destroyTime = 1f;
    public Vector3 offset = new Vector3(0, 0, 0);
    public Color Color;
    void Start()
    {
        Destroy(gameObject, destroyTime);
        offset.x = Random.Range(-1f, 1f);
        transform.localPosition += offset;
    }
}
