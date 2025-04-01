using UnityEngine;

public class ToggleObjectWithKey : MonoBehaviour
{
    static bool toggled = true;
    static ToggleObjectWithKey lastInstance;

    [SerializeField]
    GameObject objectToDisable;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    bool lastToggled = true;
    // Update is called once per frame
    void Update()
    {
        if (lastInstance == null)
            lastInstance = this;

        if (lastInstance == this)
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                toggled = !toggled;
            }
        }
        if (lastToggled != toggled)
        {
            objectToDisable.SetActive(toggled);
            lastToggled = toggled;
        }
        
        
    }
}
