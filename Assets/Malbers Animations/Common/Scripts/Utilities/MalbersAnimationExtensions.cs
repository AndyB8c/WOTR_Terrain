using UnityEngine;
using System.Collections;
using System;
using System.Reflection;
using MalbersAnimations;

public static class MalbersAnimationsExtensions
{
    /// <summary> Find the first transform grandchild with this name inside this transform</summary>
    public static Transform FindGrandChild(this Transform aParent, string aName)
    {
        var result = aParent.ChildContainsName(aName);
        if (result != null)
            return result;
        foreach (Transform child in aParent)
        {
            result = child.FindGrandChild(aName);
            if (result != null)
                return result;
        }
        return null;
    }

    public static Transform ChildContainsName(this Transform aParent, string aName)
    {
        foreach (Transform child in aParent)
        {
            if (child.name.Contains(aName))
                return child;
        }
        return null;
    }

    /// <summary>The GameObject is a prefab, Meaning in not in any scene</summary>

    public static bool IsPrefab(this GameObject go) => !go.scene.IsValid();

    /// <summary>Search for the Component in the hierarchy Up or Down</summary>
    public static T FindComponent<T>(this Component c)
    {
#pragma warning disable UNT0014 // Invalid type for call to GetComponent
        T Ttt = c.GetComponent<T>();
        if (Ttt != null) return Ttt;
        else
        {
            Ttt = c.GetComponentInParent<T>();

            if (Ttt != null) return Ttt;
            else
            {
                Ttt = c.GetComponentInChildren<T>();
                if (Ttt != null) return Ttt;
            }
        }

        return default;
#pragma warning restore UNT0014 // Invalid type for call to GetComponent

        //return c.GetComponent<T>() ?? c.GetComponentInParent<T>() ?? c.GetComponentInChildren<T>();
    }

    public static T FindComponent<T>(this GameObject c)
    {
#pragma warning disable UNT0014 // Invalid type for call to GetComponent
        T Ttt = c.GetComponent<T>();
        if (Ttt != null) return Ttt;
        else
        {
            Ttt = c.GetComponentInParent<T>();

            if (Ttt != null) return Ttt;
            else
            {
                Ttt = c.GetComponentInChildren<T>();
                if (Ttt != null) return Ttt;
            }
        }

        return default;
#pragma warning restore UNT0014 // Invalid type for call to GetComponent
        //return c.GetComponent<T>() ?? c.GetComponentInParent<T>() ?? c.GetComponentInChildren<T>();
    }

    /// <summary>returns the delta position from a rotation.</summary>
    public static Vector3 DeltaPositionFromRotate(this Transform transform, Vector3 point, Vector3 axis, float deltaAngle)
    {
        var pos = transform.position;
        var direction = pos - point;
        var rotation = Quaternion.AngleAxis(deltaAngle, axis);
        direction = rotation * direction;

        pos = point + direction - pos;
        pos.y = 0;                                                      //the Y is handled by the Fix Position method

        return pos;
    }

    /// <summary>returns the delta position from a rotation.</summary>
    public static Vector3 DeltaPositionFromRotate(this Transform transform, Transform platform, Quaternion deltaRotation)
    {
        var pos = transform.position;

        var direction = pos - platform.position;
        var directionAfterRotation = deltaRotation * direction;

        var NewPoint = platform.position + directionAfterRotation;


        pos = NewPoint - transform.position;

        return pos;
    }

    /// <summary>Resets the Local Position and rotation of a transform</summary>

    public static void ResetLocal(this Transform transform)
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    /// <summary>Resets the Local Position and rotation of a transform</summary>
    public static void SetLocalTransform(this Transform transform, Vector3 LocalPos, Vector3 LocalRot, Vector3 localScale)
    {
        transform.localPosition = LocalPos;
        transform.localEulerAngles = LocalRot;
        transform.localScale = localScale;
    }

    /// <summary>Resets the Local Position and rotation of a transform</summary>
    public static void SetLocalTransform(this Transform transform, TransformOffset offset)
    {
        transform.localPosition = offset.Position;
        transform.localEulerAngles = offset.Rotation;
        transform.localScale = offset.Scale;
    }

    /// <summary> Invoke with Parameters </summary>
    public static bool InvokeWithParams(this MonoBehaviour sender, string method, object args)
    {
        Type argType = null;

        if (args != null) argType = args.GetType();
      

        MethodInfo methodPtr = null;

        if (argType != null)
        {
            methodPtr = sender.GetType().GetMethod(method, new Type[] { argType });
        }
        else
        {
            methodPtr = sender.GetType().GetMethod(method);
        }

        if (methodPtr != null)
        {
            if (args != null)
            {
                var arguments = new object[1] { args };
                methodPtr.Invoke(sender, arguments);
                return true;
            }
            else
            {
                methodPtr.Invoke(sender, null);
                return true;
            }
        }

        PropertyInfo property = sender.GetType().GetProperty(method);

        if (property != null)
        {
            property.SetValue(sender, args, null);
            return true;

        }
        return false;
    }


    /// <summary>Invoke with Parameters and Delay </summary>
    public static void InvokeDelay(this MonoBehaviour behaviour, string method, object options, YieldInstruction wait)
    {
        behaviour.StartCoroutine(_invoke(behaviour, method, wait, options));
    }

    private static IEnumerator _invoke(this MonoBehaviour behaviour, string method, YieldInstruction wait, object options)
    {
        yield return wait;

        Type instance = behaviour.GetType();
        MethodInfo mthd = instance.GetMethod(method);
        mthd.Invoke(behaviour, new object[] { options });

        yield return null;
    }


    /// <summary>Invoke with Parameters for Scriptable objects</summary>
    public static void Invoke(this ScriptableObject sender, string method, object args)
    {
        var methodPtr = sender.GetType().GetMethod(method);

        if (methodPtr != null)
        {
            if (args != null)
            {
                var arguments = new object[1] { args };
                methodPtr.Invoke(sender, arguments);
            }
            else
            {
                methodPtr.Invoke(sender, null);
            }
        }
    }


    public static void SetLayer(this GameObject parent, int layer, bool includeChildren = true)
    {
        parent.layer = layer;
        if (includeChildren)
        {
            foreach (Transform trans in parent.transform.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layer;
            }
        }
    }


    /// --------------- EDITOR EXTENSIONS -------------------------

#if UNITY_EDITOR

#endif

}