using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Hanzzz.MeshDemolisher
{

public class MeshDemolisherExample : MonoBehaviour
{
    [Header("Hint: right click on the script to call Demolish and Reset\nin the editor mode.")]
    [Space]
    [SerializeField] private GameObject targetGameObject;
    [SerializeField] private Transform breakPointsParent;
    [SerializeField] private Material interiorMaterial;

    [SerializeField] private KeyCode demolishKey;

    [SerializeField] [Range(0f,1f)] private float resultScale;
    [SerializeField] private Transform resultParent;

    [SerializeField] private TMP_Text logText;

    private static MeshDemolisher meshDemolisher = new MeshDemolisher();

    private void Update()
    {
        if(!Input.GetKeyDown(demolishKey))
        {
            return;
        }

        if(targetGameObject.activeSelf)
        {
            Demolish();
        }
        else
        {
            Reset();
        }
    }

    [ContextMenu("Verify Demolish Input")]
    public void VerifyDemolishInput()
    {
        List<Transform> breakPoints = Enumerable.Range(0,breakPointsParent.childCount).Select(x=>breakPointsParent.GetChild(x)).ToList();

        // Passing this verification does not mean the input is valid.
        // Refer to the documentation to see all input requirements.
        bool res = meshDemolisher.VerifyDemolishInput(targetGameObject, breakPoints);
        if(res)
        {
            Debug.Log("Demolish input looks good.");
        }
    }

    [ContextMenu("Demolish")]
    public void Demolish()
    {
        Enumerable.Range(0,resultParent.childCount).Select(i=>resultParent.GetChild(i)).ToList().ForEach(x=>DestroyImmediate(x.gameObject));
        List<Transform> breakPoints = Enumerable.Range(0,breakPointsParent.childCount).Select(x=>breakPointsParent.GetChild(x)).ToList();

        var watch = System.Diagnostics.Stopwatch.StartNew();
        List<GameObject> res = meshDemolisher.Demolish(targetGameObject, breakPoints, interiorMaterial);
        watch.Stop();
        logText.text = $"Demolish time: {watch.ElapsedMilliseconds}ms.";

        res.ForEach(x=>x.transform.SetParent(resultParent, true));
        Enumerable.Range(0,resultParent.childCount).Select(i=>resultParent.GetChild(i)).ToList().ForEach(x=>x.localScale=resultScale*Vector3.one);

        targetGameObject.SetActive(false);
    }

    [ContextMenu("Demolish Async")]
    public async void DemolishAsync()
    {
        Enumerable.Range(0,resultParent.childCount).Select(i=>resultParent.GetChild(i)).ToList().ForEach(x=>DestroyImmediate(x.gameObject));
        List<Transform> breakPoints = Enumerable.Range(0,breakPointsParent.childCount).Select(x=>breakPointsParent.GetChild(x)).ToList();

        var watch = System.Diagnostics.Stopwatch.StartNew();
        List<GameObject> res = await meshDemolisher.DemolishAsync(targetGameObject, breakPoints, interiorMaterial);
        watch.Stop();
        logText.text = $"Demolish time: {watch.ElapsedMilliseconds}ms.";

        res.ForEach(x=>x.transform.SetParent(resultParent, true));
        Enumerable.Range(0,resultParent.childCount).Select(i=>resultParent.GetChild(i)).ToList().ForEach(x=>x.localScale=resultScale*Vector3.one);

        targetGameObject.SetActive(false);
    }

    [ContextMenu("Reset")]
    public void Reset()
    {
        //Enumerable.Range(0,breakPointsParent.childCount).Select(i=>breakPointsParent.GetChild(i)).ToList().ForEach(x=>DestroyImmediate(x.gameObject));
        Enumerable.Range(0,resultParent.childCount).Select(i=>resultParent.GetChild(i)).ToList().ForEach(x=>DestroyImmediate(x.gameObject));

        targetGameObject.SetActive(true);
    }

    public void OnValidate()
    {
        Enumerable.Range(0,resultParent.childCount).Select(i=>resultParent.GetChild(i)).ToList().ForEach(x=>x.localScale=resultScale*Vector3.one);
    }
}

}
