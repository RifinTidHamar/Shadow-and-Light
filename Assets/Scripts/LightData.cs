using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightData : MonoBehaviour
{
    public Color color;
    public float range;
    public float intensity;
    public bool baked;
    public enum shadowType { None, Hard, DynamicSoft }
    public shadowType shading;
}
