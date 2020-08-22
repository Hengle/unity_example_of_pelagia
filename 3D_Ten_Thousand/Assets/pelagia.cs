using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class Pelagia : MonoBehaviour
{
    [DllImport("pelagialib")]
    private static extern uint plg_NVersion();
    [DllImport("pelagialib")]
    private static extern System.IntPtr plg_MngCreateHandleWithJson(System.IntPtr jsonFile);
    [DllImport("pelagialib")]
    private static extern int plg_MngRemoteCall(System.IntPtr pvManage, System.IntPtr order, short orderLen, System.IntPtr value, short valueLen);
    [DllImport("pelagialib")]
    private static extern void plg_MngDestoryHandle(System.IntPtr pManage);
    [DllImport("pelagialib")]
    private static extern System.IntPtr plg_EventCreateHandle();
    [DllImport("pelagialib")]
    private static extern void plg_EventDestroyHandle(System.IntPtr pEventHandle);
    [DllImport("pelagialib")]
    private static extern int plg_EventTimeWait(System.IntPtr pEventHandle, System.Int64 sec, int nsec);
    [DllImport("pelagialib")]
    private static extern System.IntPtr plg_EventRecvAlloc(System.IntPtr pEventHandle, System.IntPtr valueLen);
    [DllImport("pelagialib")]
    private static extern void plg_EventFreePtr(System.IntPtr ptr);
    [DllImport("pelagialib")]
    private static extern  int plg_MngRemoteCallWithJson(System.IntPtr pvManage, System.IntPtr order, short orderLen, System.IntPtr eventHandle, System.IntPtr json, short jsonLen);

    private System.IntPtr p_manage;
    private System.IntPtr p_event;
    public int Init(string json)
    {
        System.IntPtr pjson = Marshal.StringToHGlobalAnsi(json);
        p_manage = plg_MngCreateHandleWithJson(pjson);

        if(p_manage == null)
        {
            return 0;
        }
        p_event = plg_EventCreateHandle();
        return 1;
    }

    public int Call(string order, string json)
    {
        System.IntPtr orderPtr = Marshal.StringToHGlobalAnsi(order);
        System.IntPtr jsonPtr = Marshal.StringToHGlobalAnsi(json);

        return plg_MngRemoteCallWithJson(p_manage, orderPtr, (short)order.Length, p_event, jsonPtr, (short)json.Length);
    }

    public void Destroy()
    {
        plg_MngDestoryHandle(p_manage);
        plg_EventDestroyHandle(p_event);
    }

    public string GetRec()
    {
        System.IntPtr retLen = Marshal.AllocHGlobal(4);
        System.IntPtr ret;
        ret = plg_EventRecvAlloc(p_event, retLen);
        int retInt = Marshal.ReadInt32(retLen, 0);
        if (retInt == 0)
        {
            return null;
        }

        string ss = Marshal.PtrToStringAnsi(ret);
        plg_EventFreePtr(ret);
        return ss;
    }

    public long MS()
    {
        return (System.DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
    }
    ~Pelagia()
    {
        plg_EventDestroyHandle(p_event);
        plg_MngDestoryHandle(p_manage);
    }
}
