using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

public class Reflec
{
    Type type;
    object instance;

    public Reflec(Type type, object instance)
    {
        this.type = type;
        this.instance = instance;
    }

    public MethodInfo GetMethod(string methodName)
    {
        return GetInstanceMethod(type, instance, methodName);
    }

    public Action GetMethodAction(string methodName, object[] args)
    {
        return new Action(delegate () { GetInstanceMethod(type, instance, methodName).Invoke(instance, args); });
    }

    public object GetField(string fieldName)
    {
        return GetInstanceField(type, instance, fieldName);
    }

    public void SetField(string fieldName, object fieldValue)
    {
        SetInstanceField(type, instance, fieldName, fieldValue);
    }

    public void AddFieldInt(string fieldName, int fieldvalue)
    {
        SetInstanceFieldMath(type, instance, fieldName, fieldvalue);
    }

    public static MethodInfo GetInstanceMethod(Type type, object instance, string methodName)
    {
        BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static;
        MethodInfo method = type.GetMethod(methodName, bindFlags);
        return method;
    }

    public static object GetInstanceField(Type type, object instance, string fieldName)
    {
        BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static;
        FieldInfo field = type.GetField(fieldName, bindFlags);
        return field.GetValue(instance);
    }

    public static void SetInstanceField(Type type, object instance, string fieldName, object newValue)
    {
        BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static;
        FieldInfo field = type.GetField(fieldName, bindFlags);
        field.SetValue(instance, newValue);
    }

    public static void SetInstanceFieldMath(Type type, object instance, string fieldName, int newValue)
    {
        BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static;
        FieldInfo field = type.GetField(fieldName, bindFlags);
        field.SetValue(instance, (int)GetInstanceField(type, instance, fieldName) + newValue);
    }
}