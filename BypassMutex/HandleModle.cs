using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static BypassMutex.Win32API;

namespace BypassMutex
{

    public static class HandleModle
    {
        const int CNST_SYSTEM_HANDLE_INFORMATION = 16;
        const uint STATUS_INFO_LENGTH_MISMATCH = 0xc0000004;

        /// <summary>
        /// 查看电脑是否是64位系统
        /// </summary>
        /// <returns>返回是否是64位系统bool值</returns>
        public static bool Is64Bits()
        {
            return Marshal.SizeOf(typeof(IntPtr)) == 8 ? true : false;
        }
        /// <summary>
        /// 获取目标进程的所有句柄信息
        /// </summary>
        /// <param name="process">目标进程</param>
        /// <returns>句柄信息列表</returns>
        public static List<Win32API.SYSTEM_HANDLE_INFORMATION> GetHandles(Process process)
        {
            uint nStatus;
            int nHandleInfoSize = 0x10000;
            IntPtr ipHandlePointer = Marshal.AllocHGlobal(nHandleInfoSize);
            int nLength = 0;
            IntPtr ipHandle = IntPtr.Zero;
            while ((nStatus = Win32API.NtQuerySystemInformation(CNST_SYSTEM_HANDLE_INFORMATION, ipHandlePointer, nHandleInfoSize, ref nLength)) == STATUS_INFO_LENGTH_MISMATCH)
            {
                nHandleInfoSize = nLength;
                Marshal.FreeHGlobal(ipHandlePointer);
                ipHandlePointer = Marshal.AllocHGlobal(nLength);
            }
            /*****************/
            /* 原代码，怀疑此处内存泄漏
            byte[] baTemp = new byte[nLength];
            Marshal.Copy(ipHandlePointer, baTemp, 0, nLength);
            long lHandleCount = 0;
            if (Is64Bits())
            {
                lHandleCount = Marshal.ReadInt64(ipHandlePointer);
                ipHandle = new IntPtr(ipHandlePointer.ToInt64() + 8);
            }
            else
            {
                lHandleCount = Marshal.ReadInt32(ipHandlePointer);
                ipHandle = new IntPtr(ipHandlePointer.ToInt32() + 4);
            }*/
            long lHandleCount = 0;
            if (Is64Bits())
            {
                lHandleCount = Marshal.ReadInt64(ipHandlePointer);
                ipHandle = new IntPtr(ipHandlePointer.ToInt64() + 8);
            }
            else
            {
                lHandleCount = Marshal.ReadInt32(ipHandlePointer);
                ipHandle = new IntPtr(ipHandlePointer.ToInt32() + 4);
            }
            /****************/
            Win32API.SYSTEM_HANDLE_INFORMATION shHandle;
            List<Win32API.SYSTEM_HANDLE_INFORMATION> lstHandles = new List<Win32API.SYSTEM_HANDLE_INFORMATION>();
            for (long lIndex = 0; lIndex < lHandleCount; lIndex++)
            {
                shHandle = new Win32API.SYSTEM_HANDLE_INFORMATION();
                int BB = ipHandle.ToString().Length;
                if (Is64Bits())
                {
                    shHandle = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                    ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle) + 8);
                }
                else
                {
                    ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle));
                    int dd = ipHandle.ToString().Length;
                    shHandle = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                }
                if (shHandle.ProcessID != process.Id) continue;
                lstHandles.Add(shHandle);
            }
            /***************加上释放**************/
            Marshal.FreeHGlobal(ipHandlePointer);
            /*************************************/
            ////Program.ShowMem(System.Reflection.MethodInfo.GetCurrentMethod().Name);
            return lstHandles;
        }
        /// <summary>
        /// 按原名称获取规则文件路径
        /// </summary>
        /// <param name="strRawName"></param>
        /// <returns>文件路径</returns>
        public static string GetRegularFileNameFromDevice(string strRawName)
        {
            string strFileName = strRawName;
            foreach (string strDrivePath in Environment.GetLogicalDrives())
            {
                StringBuilder sbTargetPath = new StringBuilder(Win32API.MAX_PATH);
                if (Win32API.QueryDosDevice(strDrivePath.Substring(0, 2), sbTargetPath, Win32API.MAX_PATH) == 0)
                {
                    ////Program.ShowMem(System.Reflection.MethodInfo.GetCurrentMethod().Name);
                    return strRawName;
                }
                string strTargetPath = sbTargetPath.ToString();
                if (strFileName.StartsWith(strTargetPath))
                {
                    strFileName = strFileName.Replace(strTargetPath, strDrivePath.Substring(0, 2));
                    break;
                }
            }
            ////Program.ShowMem(System.Reflection.MethodInfo.GetCurrentMethod().Name);
            return strFileName;
        }
        /// <summary>
        /// 获取文件路径名
        /// </summary>
        /// <param name="sYSTEM_HANDLE_INFORMATION">句柄信息列表</param>
        /// <param name="process">指定进程</param>
        /// <returns>文件路径名</returns>
        public static string GetFilePath(Win32API.SYSTEM_HANDLE_INFORMATION sYSTEM_HANDLE_INFORMATION, Process process)
        {
            IntPtr m_ipProcessHwnd = Win32API.OpenProcess(Win32API.ProcessAccessFlags.All, false, process.Id);
            IntPtr ipHandle = IntPtr.Zero;
            var objBasic = new Win32API.OBJECT_BASIC_INFORMATION();
            IntPtr ipBasic = IntPtr.Zero;
            var objObjectType = new Win32API.OBJECT_TYPE_INFORMATION();
            IntPtr ipObjectType = IntPtr.Zero;
            var objObjectName = new Win32API.OBJECT_NAME_INFORMATION();
            IntPtr ipObjectName = IntPtr.Zero;
            string strObjectTypeName = "";
            string strObjectName = "";
            int nLength = 0;
            int nReturn = 0;
            IntPtr ipTemp = IntPtr.Zero;
            if (!Win32API.DuplicateHandle(m_ipProcessHwnd, sYSTEM_HANDLE_INFORMATION.Handle, Win32API.GetCurrentProcess(), out ipHandle, 0, false, Win32API.DUPLICATE_SAME_ACCESS))
                return null;
            ipBasic = Marshal.AllocHGlobal(Marshal.SizeOf(objBasic));
            Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectBasicInformation, ipBasic, Marshal.SizeOf(objBasic), ref nLength);
            objBasic = (Win32API.OBJECT_BASIC_INFORMATION)Marshal.PtrToStructure(ipBasic, objBasic.GetType());
            Marshal.FreeHGlobal(ipBasic);
            ipObjectType = Marshal.AllocHGlobal(objBasic.TypeInformationLength);
            nLength = objBasic.TypeInformationLength;
            while ((uint)(nReturn = Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectTypeInformation, ipObjectType, nLength, ref nLength)) == Win32API.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(ipObjectType);
                ipObjectType = Marshal.AllocHGlobal(nLength);
            }
            objObjectType = (Win32API.OBJECT_TYPE_INFORMATION)Marshal.PtrToStructure(ipObjectType, objObjectType.GetType());
            ipTemp = Is64Bits() ? new IntPtr(Convert.ToInt64(objObjectType.Name.Buffer.ToString(), 10) >> 32) : objObjectType.Name.Buffer;
            strObjectTypeName = Marshal.PtrToStringUni(ipTemp, objObjectType.Name.Length >> 1);
            Marshal.FreeHGlobal(ipObjectType);
            nLength = objBasic.NameInformationLength;
            ipObjectName = Marshal.AllocHGlobal(nLength);
            while ((uint)(nReturn = Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectNameInformation, ipObjectName, nLength, ref nLength)) == Win32API.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(ipObjectName);
                ipObjectName = Marshal.AllocHGlobal(nLength);
            }
            objObjectName = (Win32API.OBJECT_NAME_INFORMATION)Marshal.PtrToStructure(ipObjectName, objObjectName.GetType());
            if (nLength < 0)
                return null;
            ipTemp = Is64Bits() ? new IntPtr(Convert.ToInt64(objObjectName.Name.Buffer.ToString(), 10) >> 32) : objObjectName.Name.Buffer;
            if (ipTemp != IntPtr.Zero)
            {
                byte[] baTemp = new byte[nLength];
                try
                {
                    Marshal.Copy(ipTemp, baTemp, 0, nLength);

                    strObjectName = Marshal.PtrToStringUni(Is64Bits() ? new IntPtr(ipTemp.ToInt64()) : new IntPtr(ipTemp.ToInt32()));
                }
                catch (AccessViolationException)
                {
                    return null;
                }
                finally
                {
                    Marshal.FreeHGlobal(ipObjectName);
                    Win32API.CloseHandle(ipHandle);
                }
            }
            try
            {
                ////Program.ShowMem(System.Reflection.MethodInfo.GetCurrentMethod().Name);
                return GetRegularFileNameFromDevice(strObjectName);
            }
            catch
            {
                return null;
            }
        }




        public static void FindAndCloseWeChatMutexHandle(SYSTEM_HANDLE_INFORMATION systemHandleInformation, Process process)
        {
            IntPtr ipHandle = IntPtr.Zero;
            IntPtr openProcessHandle = IntPtr.Zero;
            IntPtr hObjectName = IntPtr.Zero;

            try
            {
                ProcessAccessFlags flags = ProcessAccessFlags.DupHandle | ProcessAccessFlags.VMRead;
                openProcessHandle = OpenProcess(flags, false, process.Id);
                // 通过 DuplicateHandle 访问句柄
                if (!DuplicateHandle(openProcessHandle, systemHandleInformation.Handle, GetCurrentProcess(), out ipHandle, 0, false, DUPLICATE_SAME_ACCESS))
                {
                    return;
                }

                int nLength = 0;
                hObjectName = Marshal.AllocHGlobal(256 * 1024);

                // 查询句柄名称
                while ((uint)(NtQueryObject(ipHandle, (int)ObjectInformationClass.ObjectNameInformation, hObjectName, nLength, ref nLength)) == STATUS_INFO_LENGTH_MISMATCH)
                {
                    Marshal.FreeHGlobal(hObjectName);
                    if (nLength == 0)
                    {
                        Console.WriteLine("Length returned at zero!");
                        return;
                    }
                    hObjectName = Marshal.AllocHGlobal(nLength);
                }
                OBJECT_NAME_INFORMATION objObjectName = new OBJECT_NAME_INFORMATION();
                objObjectName = (OBJECT_NAME_INFORMATION)Marshal.PtrToStructure(hObjectName, objObjectName.GetType());

                if (objObjectName.Name.Buffer != IntPtr.Zero)
                {
                    string strObjectName = Marshal.PtrToStringUni(objObjectName.Name.Buffer);
                    Console.WriteLine(strObjectName);
                    //  \Sessions\1\BaseNamedObjects\_WeChat_App_Instance_Identity_Mutex_Name
                    if (strObjectName.Contains("_Instance_Identity_Mutex_Name"))
                    {
                        // 通过 DuplicateHandle DUPLICATE_CLOSE_SOURCE 关闭句柄
                        IntPtr mHandle = IntPtr.Zero;
                        if (DuplicateHandle(openProcessHandle, systemHandleInformation.Handle, GetCurrentProcess(), out mHandle, 0, false, DUPLICATE_CLOSE_SOURCE))
                        {
                            CloseHandle(mHandle);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(hObjectName);
                CloseHandle(ipHandle);
                CloseHandle(openProcessHandle);
            }
        }

    }
    }
