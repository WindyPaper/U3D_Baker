using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;


public class ucThreadDispatcher
{
    public void RunOnMainThread(Action action)
    {
        //lock (_backlog)
        //{
        //    _backlog.Add(action);
        //    _queued = true;
        //}
    }

    public static ucThreadDispatcher Initialize()
    {
        if (_instance == null)
        {
            Debug.Log("Init ucThreadDispatcher Manager!");
            _instance = new ucThreadDispatcher();
        }
        return _instance;
    }

    //public void Update()
    //{
    //    if (_queued)
    //    {
    //        lock (_backlog)
    //        {
    //            var tmp = _actions;
    //            _actions = _backlog;
    //            _backlog = tmp;
    //            _queued = false;
    //        }

    //        foreach (var action in _actions)
    //            action();

    //        _actions.Clear();
    //    }
    //}

    static ucThreadDispatcher _instance;
    //static volatile bool _queued = false;
    //static List<Action> _backlog = new List<Action>(8);
    //static List<Action> _actions = new List<Action>(8);
}