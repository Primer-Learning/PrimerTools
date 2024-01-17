using System.Collections;
using System.Collections.Generic;
using Primer;
using UnityEngine;

public class AccessoryDisplay : MonoBehaviour
{
    // Makes a grid of blobs showing color and accessory options
    [SerializeField] PrimerBlob testBlob;
    float xSpacing = 2;
    float ySpacing = 3;
    void Start()
    {   
        List<Color> blobColors = new List<Color>() {
            PrimerColor.blue,
            PrimerColor.orange,
            PrimerColor.yellow,
            PrimerColor.red,
            PrimerColor.green,
            PrimerColor.purple
        };
        var aTypes = (AccessoryType[])System.Enum.GetValues(typeof(AccessoryType));

        for (int x = 0; x < aTypes.Length; x++) {
            for (int y = 0; y < blobColors.Count; y++) {
                PrimerBlob b = Instantiate(testBlob);
                b.transform.localPosition = new Vector3(x * xSpacing, y * ySpacing, 0);
                b.SetColor(blobColors[y]);
                b.ScaleUpFromZero();
                b.AddAccessory(aTypes[x], colorMatch:true, animate: true);
                b.AddAccessory(AccessoryType.fairSign, animate: true);
            }
        }
    }
}
