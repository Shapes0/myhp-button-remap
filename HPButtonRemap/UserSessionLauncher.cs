using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ComponentModel;

namespace HPButtonRemap;

/// <summary>
/// Helper to launch processes in the active user session from a Windows Service (Session 0)
/// </summary>
public static class UserSessionLauncher
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessAsUser(
        IntPtr hToken,
        string lpApplicationName,
        string? lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DuplicateTokenEx(
        IntPtr hExistingToken,
        uint dwDesiredAccess,
        IntPtr lpTokenAttributes,
        SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
        TOKEN_TYPE TokenType,
        out IntPtr phNewToken);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQueryUserToken(uint SessionId, out IntPtr phToken);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(
        IntPtr TokenHandle,
        bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState,
        uint BufferLength,
        IntPtr PreviousState,
        IntPtr ReturnLength);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool LookupPrivilegeValue(
        string? lpSystemName,
        string lpName,
        out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(
        IntPtr ProcessHandle,
        uint DesiredAccess,
        out IntPtr TokenHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    private const uint TOKEN_DUPLICATE = 0x0002;
    private const uint TOKEN_QUERY = 0x0008;
    private const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint GENERIC_ALL = 0x10000000;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private const uint CREATE_NO_WINDOW = 0x08000000;
    private const uint CREATE_NEW_CONSOLE = 0x00000010;
    private const uint NORMAL_PRIORITY_CLASS = 0x00000020;
    private const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
    private const string SE_ASSIGNPRIMARYTOKEN_NAME = "SeAssignPrimaryTokenPrivilege";
    private const uint SE_PRIVILEGE_ENABLED = 0x00000002;

    private enum SECURITY_IMPERSONATION_LEVEL
    {
        SecurityAnonymous,
        SecurityIdentification,
        SecurityImpersonation,
        SecurityDelegation
    }

    private enum TOKEN_TYPE
    {
        TokenPrimary = 1,
        TokenImpersonation
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    /// <summary>
    /// Enable required privileges for the process
    /// </summary>
    private static bool EnablePrivilege(string privilegeName)
    {
        IntPtr tokenHandle = IntPtr.Zero;
        try
        {
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandle))
            {
                return false;
            }

            LUID luid;
            if (!LookupPrivilegeValue(null, privilegeName, out luid))
            {
                return false;
            }

            TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SE_PRIVILEGE_ENABLED
            };

            return AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            if (tokenHandle != IntPtr.Zero)
                CloseHandle(tokenHandle);
        }
    }

    /// <summary>
    /// Launch a process in the active user session (even when called from a service in Session 0)
    /// </summary>
    public static bool LaunchProcessInUserSession(string applicationPath, string arguments, out string error)
    {
        error = string.Empty;
        IntPtr userToken = IntPtr.Zero;
        IntPtr duplicateToken = IntPtr.Zero;
        IntPtr environmentBlock = IntPtr.Zero;

        try
        {
            // Enable required privileges
            EnablePrivilege(SE_INCREASE_QUOTA_NAME);
            EnablePrivilege(SE_ASSIGNPRIMARYTOKEN_NAME);

            // Get the session ID of the active console session
            uint sessionId = WTSGetActiveConsoleSessionId();
            if (sessionId == 0xFFFFFFFF)
            {
                error = "No active console session found";
                return false;
            }

            // Get the user token for the active session
            if (!WTSQueryUserToken(sessionId, out userToken))
            {
                int lastError = Marshal.GetLastWin32Error();
                error = $"WTSQueryUserToken failed with error code {lastError}: {new Win32Exception(lastError).Message}";
                return false;
            }

            // Duplicate the token
            if (!DuplicateTokenEx(
                userToken,
                GENERIC_ALL,
                IntPtr.Zero,
                SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                TOKEN_TYPE.TokenPrimary,
                out duplicateToken))
            {
                int lastError = Marshal.GetLastWin32Error();
                error = $"DuplicateTokenEx failed with error code {lastError}: {new Win32Exception(lastError).Message}";
                return false;
            }

            // Create environment block for the user
            if (!CreateEnvironmentBlock(out environmentBlock, duplicateToken, false))
            {
                int lastError = Marshal.GetLastWin32Error();
                error = $"CreateEnvironmentBlock failed with error code {lastError}: {new Win32Exception(lastError).Message}";
                return false;
            }

            // Prepare startup info
            STARTUPINFO startupInfo = new STARTUPINFO
            {
                cb = Marshal.SizeOf(typeof(STARTUPINFO)),
                lpDesktop = "winsta0\\default"
            };

            PROCESS_INFORMATION processInfo;

            // For CreateProcessAsUser:
            // - lpApplicationName should be the full path to the executable
            // - lpCommandLine should be null or just the arguments (mutable buffer)
            // This avoids issues with command line parsing
            string? commandLine = string.IsNullOrEmpty(arguments) ? null : arguments;

            // Launch the process as the user
            bool result = CreateProcessAsUser(
                duplicateToken,
                applicationPath,
                commandLine!,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                CREATE_UNICODE_ENVIRONMENT | NORMAL_PRIORITY_CLASS,
                environmentBlock,
                Path.GetDirectoryName(applicationPath) ?? string.Empty,
                ref startupInfo,
                out processInfo);

            if (!result)
            {
                int lastError = Marshal.GetLastWin32Error();
                error = $"CreateProcessAsUser failed with error code {lastError}: {new Win32Exception(lastError).Message}. App: {applicationPath}, Args: {arguments ?? "(none)"}";
                return false;
            }

            // Close process and thread handles (we don't need them)
            CloseHandle(processInfo.hProcess);
            CloseHandle(processInfo.hThread);

            return true;
        }
        catch (Exception ex)
        {
            error = $"Exception: {ex.Message}";
            return false;
        }
        finally
        {
            if (environmentBlock != IntPtr.Zero)
                DestroyEnvironmentBlock(environmentBlock);
            if (duplicateToken != IntPtr.Zero)
                CloseHandle(duplicateToken);
            if (userToken != IntPtr.Zero)
                CloseHandle(userToken);
        }
    }

    /// <summary>
    /// Check if we're running as a Windows Service (in Session 0)
    /// </summary>
    public static bool IsRunningAsService()
    {
        return Process.GetCurrentProcess().SessionId == 0;
    }
}
