using System.Reflection;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Inst {
        get {
            if ( applicationIsQuitting ) {
                Debug.LogWarning( "[Singleton] Instance '" + typeof( T ) + "' already destroyed on application quit." + " Won't create again - returning null." );
                return null;
            }

            if ( null != _instance ) {
                return _instance;
            }

            _instance = (T)FindObjectOfType( typeof( T ) );

            if ( FindObjectsOfType( typeof( T ) ).Length > 1 ) {
                Debug.LogError( "[Singleton] Something went really wrong " + " - there should never be more than 1 singleton!" + " Reopening the scene might fix it." );
                return _instance;
            }

            if ( _instance == null ) {
                GameObject singleton = new GameObject( typeof( T ).ToString() );
                _instance = singleton.AddComponent<T>();
                DontDestroyOnLoad( singleton );
            }
            else {
                Debug.LogError( "[Singleton] Using instance already created: " + _instance.gameObject.name );
            }

            return _instance;
        }
    }


    public static void DestroyInst ()
    {
        //Debug.Log( string.Format( "{0}.{1}.{2}", _instance.gameObject, "Singleton", MethodBase.GetCurrentMethod().Name ) );

        if ( _instance == null ) {
            return;
        }

        GameObject.Destroy( _instance.gameObject );

        _instance = null;
    }


    public static bool Exist { get { return _instance != null; } }


    private static bool applicationIsQuitting = false;
    /// <summary>
    /// When Unity quits, it destroys objects in a random order.
    /// In principle, a Singleton is only destroyed when application quits.
    /// If any script calls Instance after it have been destroyed, 
    ///   it will create a buggy ghost object that will stay on the Editor scene
    ///   even after stopping playing the Application. Really bad!
    /// So, this was made to be sure we're not creating that buggy ghost object.
    /// </summary>
    public virtual void OnDestroy ()
    {
        //Debug.Log( string.Format( "{0}.{1}.{2}", name, GetType().Name, MethodBase.GetCurrentMethod().Name ) );

        applicationIsQuitting = true;
    }
}