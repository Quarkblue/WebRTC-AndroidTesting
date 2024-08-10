using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class InternetPermission : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission("android.permission.INTERNET"))
        {
            Permission.RequestUserPermission("android.permission.INTERNET");
        }
        #endif
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
