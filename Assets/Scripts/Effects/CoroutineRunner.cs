using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineWrap
{
    public Coroutine Coroutine;
    public object[] Data;

    public CoroutineWrap(Coroutine coroutine, object[] data)
    {
        Coroutine = coroutine;
        Data = data;
    }
}

public class CoroutineRunner : MonoBehaviour
{
    [NonSerialized] public List<CoroutineWrap> coroutines = new();

    public CoroutineWrap Run(IEnumerator routine, object[] data)
    {
        CoroutineWrap wrapper = new CoroutineWrap(null, data);

        wrapper.Coroutine = StartCoroutine(RunInternal(routine, wrapper));

        coroutines.Add(wrapper);

        return wrapper;
    }

    private IEnumerator RunInternal(IEnumerator routine, CoroutineWrap wrapper)
    {
        yield return routine;

        coroutines.Remove(wrapper);
    }

    public void Stop(CoroutineWrap wrapper)
    {
        StopCoroutine(wrapper.Coroutine);
        coroutines.Remove(wrapper);
    }
}