# BypassMutex
程序多开例子

移除指定程序Mutex互斥进程
详情见FindAndCloseWeChatMutexHandle方法

tip:
Marshal.PtrToStringUni貌似anycpu不可用,需指定x86
微信的进程互斥名"\Sessions\1\BaseNamedObjects\_WeChat_App_Instance_Identity_Mutex_Name"其他程序需修改对应名称
查看互斥名工具xuetr