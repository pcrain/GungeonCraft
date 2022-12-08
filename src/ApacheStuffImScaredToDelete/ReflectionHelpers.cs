using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CwaffingTheGungy {

    public static class ReflectionHelpers {

        public static IList CreateDynamicList(Type type) {
            bool flag = type == null;
            if (flag) { throw new ArgumentNullException("type", "Argument cannot be null."); }
            ConstructorInfo[] constructors = typeof(List<>).MakeGenericType(new Type[] { type }).GetConstructors();
            foreach (ConstructorInfo constructorInfo in constructors) {
                ParameterInfo[] parameters = constructorInfo.GetParameters();
                bool flag2 = parameters.Length != 0;
                if (!flag2) { return (IList)constructorInfo.Invoke(null, null); }
            }
            throw new ApplicationException("Could not create a new list with type <" + type.ToString() + ">.");
        }
        
        public static IDictionary CreateDynamicDictionary(Type typeKey, Type typeValue) {
            bool flag = typeKey == null;
            if (flag) {
                throw new ArgumentNullException("type_key", "Argument cannot be null.");
            }
            bool flag2 = typeValue == null;
            if (flag2) { throw new ArgumentNullException("type_value", "Argument cannot be null."); }
            ConstructorInfo[] constructors = typeof(Dictionary<,>).MakeGenericType(new Type[] { typeKey, typeValue }).GetConstructors();
            foreach (ConstructorInfo constructorInfo in constructors) {
                ParameterInfo[] parameters = constructorInfo.GetParameters();
                bool flag3 = parameters.Length != 0;
                if (!flag3) { return (IDictionary)constructorInfo.Invoke(null, null); }
            }
            throw new ApplicationException(string.Concat(new string[] {
                "Could not create a new dictionary with types <",
                typeKey.ToString(),
                ",",
                typeValue.ToString(),
                ">."
            }));
        }

        public static T ReflectGetField<T>(Type classType, string fieldName, object o = null) {
            FieldInfo field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | ((o != null) ? BindingFlags.Instance : BindingFlags.Static));
            return (T)field.GetValue(o);
        }

        public static void ReflectSetField<T>(Type classType, string fieldName, T value, object o = null) {
            FieldInfo field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | ((o != null) ? BindingFlags.Instance : BindingFlags.Static));
            field.SetValue(o, value);
        }

        public static T ReflectGetProperty<T>(Type classType, string propName, object o = null, object[] indexes = null) {
            PropertyInfo property = classType.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | ((o != null) ? BindingFlags.Instance : BindingFlags.Static));
            return (T)property.GetValue(o, indexes);
        }

        public static void ReflectSetProperty<T>(Type classType, string propName, T value, object o = null, object[] indexes = null) {
            PropertyInfo property = classType.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | ((o != null) ? BindingFlags.Instance : BindingFlags.Static));
            property.SetValue(o, value, indexes);
        }

        public static MethodInfo ReflectGetMethod(Type classType, string methodName, Type[] methodArgumentTypes = null, Type[] genericMethodTypes = null, bool? isStatic = null) {
            MethodInfo[] array = ReflectTryGetMethods(classType, methodName, methodArgumentTypes, genericMethodTypes, isStatic);
            bool flag = array.Count() == 0;
            if (flag) { throw new MissingMethodException("Cannot reflect method, not found based on input parameters."); }
            bool flag2 = array.Count() > 1;
            if (flag2) { throw new InvalidOperationException("Cannot reflect method, more than one method matched based on input parameters."); }
            return array[0];
        }

        public static MethodInfo ReflectTryGetMethod(Type classType, string methodName, Type[] methodArgumentTypes = null, Type[] genericMethodTypes = null, bool? isStatic = null) {
            MethodInfo[] array = ReflectTryGetMethods(classType, methodName, methodArgumentTypes, genericMethodTypes, isStatic);
            bool flag = array.Count() == 0;
            MethodInfo result;
            if (flag) {
                result = null;
            } else {
                bool flag2 = array.Count() > 1;
                if (flag2) { result = null; } else { result = array[0]; }
            }
            return result;
        }

        public static MethodInfo[] ReflectTryGetMethods(Type classType, string methodName, Type[] methodArgumentTypes = null, Type[] genericMethodTypes = null, bool? isStatic = null) {
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
            bool flag = isStatic == null || isStatic.Value;
            if (flag) { bindingFlags |= BindingFlags.Static; }
            bool flag2 = isStatic == null || !isStatic.Value;
            if (flag2) { bindingFlags |= BindingFlags.Instance; }
            MethodInfo[] methods = classType.GetMethods(bindingFlags);
            List<MethodInfo> list = new List<MethodInfo>();
            for (int i = 0; i < methods.Length; i++) {
            // foreach (MethodInfo methodInfo in methods) {
                bool flag3 = methods[i].Name != methodName;
                if (!flag3) {
                    bool isGenericMethodDefinition = methods[i].IsGenericMethodDefinition;
                    if (isGenericMethodDefinition) {
                        bool flag4 = genericMethodTypes == null || genericMethodTypes.Length == 0;
                        if (flag4) { goto IL_14D; }
                        Type[] genericArguments = methods[i].GetGenericArguments();
                        bool flag5 = genericArguments.Length != genericMethodTypes.Length;
                        if (flag5) { goto IL_14D; }
                        methods[i] = methods[i].MakeGenericMethod(genericMethodTypes);
                    } else {
                        bool flag6 = genericMethodTypes != null && genericMethodTypes.Length != 0;
                        if (flag6) { goto IL_14D; }
                    }
                    ParameterInfo[] parameters = methods[i].GetParameters();
                    bool flag7 = methodArgumentTypes != null;
                    if (!flag7) { goto IL_141; }
                    bool flag8 = parameters.Length != methodArgumentTypes.Length;
                    if (!flag8) {
                        for (int j = 0; j < parameters.Length; j++) {
                            ParameterInfo parameterInfo = parameters[j];
                            bool flag9 = parameterInfo.ParameterType != methodArgumentTypes[j];
                            if (flag9) { goto IL_14A; }
                        }
                        goto IL_141;
                    }
                    IL_14A:
                    goto IL_14D;
                    IL_141:
                    list.Add(methods[i]);
                }
                IL_14D:;
            }
            return list.ToArray();
        }

        public static T InvokeMethod<T>(Type type, string methodName, object typeInstance = null, object[] methodParams = null) {
            BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | (typeInstance == null ? BindingFlags.Static : BindingFlags.Instance);
            return (T)type.GetMethod(methodName, bindingFlags).Invoke(typeInstance, methodParams);
        }

        public static void InvokeMethod(Type type, string methodName, object typeInstance = null, object[] methodParams = null) {
            BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | (typeInstance == null ? BindingFlags.Static : BindingFlags.Instance);
            type.GetMethod(methodName, bindingFlags).Invoke(typeInstance, methodParams);
        }

        public static object InvokeRefs<T0>(MethodInfo methodInfo, object o, T0 p0) {
            object[] parameters = new object[] { p0 };
            return methodInfo.Invoke(o, parameters);
        }

        public static object InvokeRefs<T0>(MethodInfo methodInfo, object o, ref T0 p0) {
            object[] array = new object[] { p0 };
            object result = methodInfo.Invoke(o, array);
            p0 = (T0)array[0];
            return result;
        }

        public static object InvokeRefs<T0, T1>(MethodInfo methodInfo, object o, T0 p0, T1 p1) {
            object[] parameters = new object[] { p0, p1 };
            return methodInfo.Invoke(o, parameters);
        }

        public static object InvokeRefs<T0, T1>(MethodInfo methodInfo, object o, ref T0 p0, T1 p1) {
            object[] array = new object[] { p0, p1 };
            object result = methodInfo.Invoke(o, array);
            p0 = (T0)array[0];
            return result;
        }

        public static object InvokeRefs<T0, T1>(MethodInfo methodInfo, object o, T0 p0, ref T1 p1) {
            object[] array = new object[] { p0, p1 };
            object result = methodInfo.Invoke(o, array);
            p1 = (T1)array[1];
            return result;
        }

        public static object InvokeRefs<T0, T1>(MethodInfo methodInfo, object o, ref T0 p0, ref T1 p1) {
            object[] array = new object[] { p0, p1 };
            object result = methodInfo.Invoke(o, array);
            p0 = (T0)array[0];
            p1 = (T1)array[1];
            return result;
        }

        public static object InvokeRefs<T0, T1, T2>(MethodInfo methodInfo, object o, T0 p0, T1 p1, T2 p2) {
            object[] parameters = new object[] { p0, p1, p2 };
            return methodInfo.Invoke(o, parameters);
        }

        public static object InvokeRefs<T0, T1, T2>(MethodInfo methodInfo, object o, ref T0 p0, T1 p1, T2 p2) {
            object[] array = new object[] { p0, p1, p2 };
            object result = methodInfo.Invoke(o, array);
            p0 = (T0)array[0];
            return result;
        }

        public static object InvokeRefs<T0, T1, T2>(MethodInfo methodInfo, object o, T0 p0, ref T1 p1, T2 p2) {
            object[] array = new object[] { p0, p1, p2 };
            object result = methodInfo.Invoke(o, array);
            p1 = (T1)array[1];
            return result;
        }

        public static object InvokeRefs<T0, T1, T2>(MethodInfo methodInfo, object o, T0 p0, T1 p1, ref T2 p2) {
            object[] array = new object[] { p0, p1, p2 };
            object result = methodInfo.Invoke(o, array);
            p2 = (T2)array[2];
            return result;
        }

        public static object InvokeRefs<T0, T1, T2>(MethodInfo methodInfo, object o, ref T0 p0, ref T1 p1, T2 p2) {
            object[] array = new object[] { p0, p1, p2 };
            object result = methodInfo.Invoke(o, array);
            p0 = (T0)array[0];
            p1 = (T1)array[1];
            return result;
        }

        public static object InvokeRefs<T0, T1, T2>(MethodInfo methodInfo, object o, ref T0 p0, T1 p1, ref T2 p2) {
            object[] array = new object[] { p0, p1, p2 };
            object result = methodInfo.Invoke(o, array);
            p0 = (T0)array[0];
            p2 = (T2)array[2];
            return result;
        }

        public static object InvokeRefs<T0, T1, T2>(MethodInfo methodInfo, object o, T0 p0, ref T1 p1, ref T2 p2) {
            object[] array = new object[] { p0, p1, p2 };
            object result = methodInfo.Invoke(o, array);
            p1 = (T1)array[1];
            p2 = (T2)array[2];
            return result;
        }

        public static object InvokeRefs<T0, T1, T2>(MethodInfo methodInfo, object o, ref T0 p0, ref T1 p1, ref T2 p2) {
            object[] array = new object[] { p0, p1, p2 };
            object result = methodInfo.Invoke(o, array);
            p0 = (T0)array[0];
            p1 = (T1)array[1];
            p2 = (T2)array[2];
            return result;
        }
    }
}

