using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PrimerBlob))]
public class AccessoryManager : MonoBehaviour
{
    public AccessoryType accessoryType;
    public bool colorMatch;
    private PrimerBlob Blob => GetComponent<PrimerBlob>();

    private void OnValidate()
    {
        Blob.DestroyAccessories();
        Blob.AddAccessory(accessoryType, colorMatch: colorMatch);
    }
}
