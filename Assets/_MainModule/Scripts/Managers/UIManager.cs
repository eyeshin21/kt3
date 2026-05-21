using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelInstance<T> where T : MonoBehaviour
{
    private T _panel;
    public T Panel
    {
        get
        {
            if (_panel == null)
            {
                var child = (RectTransform)UIManager.Instance.transform.Find(prefabName);

                if (child == null)
                {
                    child = GenerateNewSlotPopup(prefabName);

                }
                _panel = child.gameObject.GetComponentInChildren<T>();
                if (_panel == null)
                {
                    var prefab = ((GameObject)Resources.Load($"Prefabs/UI/{prefabName}"));
                    _panel = GameObject.Instantiate(prefab, child).GetComponent<T>();
                }
            }
            return _panel;
        }
    }

    private RectTransform GenerateNewSlotPopup(string slotName)
    {
        var child = new GameObject(slotName, typeof(RectTransform)).GetComponent<RectTransform>();
        child.SetParent(UIManager.Instance.transform);
        child.offsetMax = Vector2.zero;
        child.anchorMax = Vector2.one;
        child.offsetMin = Vector2.zero;
        child.anchorMin = Vector2.zero;
        child.localScale = Vector3.one;

        return child;
    }

    string prefabName;

    public PanelInstance(string prefabName)
    {
        this.prefabName = prefabName;
    }

}

public class UIManager : SingletonMono<UIManager>
{
    private Dictionary<string, PanelInstance<PanelBase>> _panelInstances = new Dictionary<string, PanelInstance<PanelBase>>();

    public T GetPanel<T>() where T : MonoBehaviour
    {
        MonoBehaviour res = null;

        if(!_panelInstances.ContainsKey(typeof(T).Name))
        {
            _panelInstances.Add(typeof(T).Name, new PanelInstance<PanelBase>(typeof(T).Name));
        }    

        res = _panelInstances[typeof(T).Name].Panel.GetComponent<T>();

        if(res == null) throw new System.Exception($"Panel {typeof(T).Name} does not exist in the prefab or is not a child of the prefab.");

        return (T)res;
    } 
        

    private void Start()
    {
        GetPanel<UIHome>().Show();
    }
}
