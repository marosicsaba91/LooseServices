﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{

public static class ServiceLocator
{
    static readonly ServiceEnvironment environment = new ServiceEnvironment();
    public static ServiceEnvironment Environment => environment;

    internal static IEnumerable<SceneServiceInstaller> SceneInstallers => environment.SceneInstallers;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    internal static void UpdateGlobalInstallers()
    {
        environment.globalInstallers = FindGlobalInstallers;
    }

    static List<ServiceSourceSet> FindGlobalInstallers =>
        Resources
            .LoadAll<ServiceSourceSet>(string.Empty)
            .Where(contextInstaller => contextInstaller.useAsGlobalInstaller)
            .OrderByDescending(set => set.priority)
            .ToList();


    static Transform _parentObject;

    public static Transform ParentObject
    {
        get
        {
            if (_parentObject == null)
            {
                _parentObject = new GameObject("Services").transform;
                Object.DontDestroyOnLoad(_parentObject.gameObject);
            }

            return _parentObject;
        }
    }

    internal static void ClearAllCachedData()
    {
        foreach (var installerSourcePair in Environment.SceneAndGlobalContextServiceSources)
            installerSourcePair.source.GetDynamicServiceSource()?.ClearInstancesAndCachedTypes(); 
        environment.InvokeLoadedInstancesChanged();
    }

    public static TService Get<TService>(params object[] tags) =>
        (TService) Get(typeof(TService), tags);

    public static object Get(Type looseServiceType, params object[] tags)
    {
        if (TryGet(looseServiceType, tags, out object service))
            return service;

        throw CantFindService(looseServiceType);
    }

    public static bool TryGet<TService>(out TService service) =>
        TryGet(tags: null, out service);

    public static bool TryGet(Type looseServiceType, out object service) =>
        TryGet(looseServiceType, tags: null, out service);

    public static bool TryGet<TService>(object[] tags, out TService service)
    {
        if (TryGet(typeof(TService), tags, out object service1))
        {
            service = (TService) service1;
            return true;
        }

        service = default;
        return false;
    }

    public static bool TryGet(Type looseServiceType, object[] tags, out object service)
    {
        foreach ((IServiceSourceSet installer, ServiceSource source) in Environment.SceneAndGlobalContextServiceSources)
        {
            DynamicServiceSource dynamicSource = source?.GetDynamicServiceSource();
            if (dynamicSource == null) continue;
            bool typeEnabled = dynamicSource.GetAllAbstractTypes().Contains(looseServiceType);

            if (!typeEnabled)
                foreach (SerializableType variable in source.additionalTypes)
                {
                    if (typeEnabled) break;
                    if (variable.Type == looseServiceType) typeEnabled = true;
                }

            if (!typeEnabled) continue;

            if (TryGetServiceInSource(looseServiceType, installer, source, dynamicSource, tags, out object serv))
            {
                service = serv;
                return true;
            }
        }

        service = null;
        return false;
    }

    static bool TryGetServiceInSource(
        Type looseServiceType,
        IServiceSourceSet set,
        ServiceSource source,
        DynamicServiceSource dynamicSource,
        object[] tags, out object service)
    {
        service = null;

        if (!dynamicSource.TryGetService(looseServiceType, set, tags, source.tags, out service,
            out bool newInstance))
            return false;
        if (newInstance)
            Environment.InvokeLoadedInstancesChanged();
        return true;
    }

    static Exception CantFindService(Type looseServiceType) =>
        new ArgumentException($"Can't find Services of this Type: {looseServiceType}");

    internal static IEnumerable<IServiceSourceSet> GetInstallers()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UpdateGlobalInstallers();
#endif

        foreach (IServiceSourceSet installer in Environment.GetInstallers())
            yield return installer; 
    }

}
}