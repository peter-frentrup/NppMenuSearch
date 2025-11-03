#include <Windows.h>
#include <Vcclr.h>

#using <NppMenuSearchImpl.dll>


struct NppData
{
	HWND _nppHandle;
	HWND _scintillaMainHandle;
	HWND _scintillaSecondHandle;
};


#define NPPN_FIRST           (1000)
#define NPPN_SHUTDOWN        (NPPN_FIRST + 9)


using namespace System;
using namespace System::Reflection;
using namespace Runtime::InteropServices;

static __declspec(noinline) void InitAssemblyResolve() {
	static bool isAssemblyResolveInitialized = false;

	if (isAssemblyResolveInitialized)
		return;

	Assembly^ currentAssembly = Assembly::GetExecutingAssembly();
	String ^dir = IO::Path::GetDirectoryName(currentAssembly->Location);

	AppDomain::CurrentDomain->AppendPrivatePath(dir);

	isAssemblyResolveInitialized = true;
}


static const wchar_t* g_ptrPluginName = nullptr;

static __declspec(noinline) const wchar_t* getNameImpl()
{
	//Console::WriteLine("getNameImpl {0}", NppMenuSearch::UnmanagedExports::GetName());
	//return L"NppMenuSearchStartup";
	if (!g_ptrPluginName) {
		String^ name = NppMenuSearch::UnmanagedExports::GetName();
		if (name)
			g_ptrPluginName = (const wchar_t*)(void*)Marshal::StringToHGlobalUni(name);
	}
	return g_ptrPluginName;
}

static __declspec(noinline) void setInfoImpl(::NppData notepadPlusData)
{
	NppMenuSearch::UnmanagedExports::SetInfo((IntPtr)notepadPlusData._nppHandle, (IntPtr)notepadPlusData._scintillaMainHandle, (IntPtr)notepadPlusData._scintillaSecondHandle);
}

static __declspec(noinline) void* getFuncsArrayImpl(int* nbF)
{
	int n = 0;
	IntPtr result = NppMenuSearch::UnmanagedExports::GetFuncsArray(n);
	*nbF = n;
	return (void*)result;
}

static __declspec(noinline) void beNotifiedImpl(NMHDR *notification)
{
	NppMenuSearch::UnmanagedExports::BeNotified((IntPtr)notification);

	if (notification->code == NPPN_SHUTDOWN) {
		Marshal::FreeHGlobal((IntPtr)(void*)g_ptrPluginName);
		g_ptrPluginName = nullptr;
	}
}

/*======================================================================================================================================*/

extern "C" __declspec(dllexport) BOOL __cdecl isUnicode() {
#ifndef NDEBUG
	MessageBoxExW(nullptr, L"isUnicode called", L"NppMenuSearch", 0, 0);
#endif
	return TRUE;
}

extern "C" __declspec(dllexport) const wchar_t* __cdecl getName()
{
#ifndef NDEBUG
	MessageBoxExW(nullptr, L"getName called", L"NppMenuSearch", 0, 0);
#endif
	try {
		InitAssemblyResolve();
		return getNameImpl();
	}
	catch (Exception^ ex) {
		cli::pin_ptr<const Char> msg = PtrToStringChars(ex->ToString());
		MessageBoxExW(nullptr, msg, L"NppMenuSearch", 0, 0);
		throw;
	}
}

extern "C" __declspec(dllexport) void __cdecl setInfo(::NppData notepadPlusData)
{
#ifndef NDEBUG
	MessageBoxExW(nullptr, L"setInfo called", L"NppMenuSearch", 0, 0);
#endif
	try {
		InitAssemblyResolve();
		setInfoImpl(notepadPlusData);
	}
	catch (Exception^ ex) {
		cli::pin_ptr<const Char> msg = PtrToStringChars(ex->ToString());
		MessageBoxExW(nullptr, msg, L"NppMenuSearch", 0, 0);
		throw;
	}
}

extern "C" __declspec(dllexport) void* __cdecl getFuncsArray(int* nbF)
{
#ifndef NDEBUG
	MessageBoxExW(nullptr, L"getFuncsArray called", L"NppMenuSearch", 0, 0);
#endif
	try {
		InitAssemblyResolve();
		return getFuncsArrayImpl(nbF);
	}
	catch (Exception^ ex) {
		cli::pin_ptr<const Char> msg = PtrToStringChars(ex->ToString());
		MessageBoxExW(nullptr, msg, L"NppMenuSearch", 0, 0);
		throw;
	}
}

extern "C" __declspec(dllexport) void __cdecl beNotified(NMHDR* notification)
{
	try {
		beNotifiedImpl(notification);
	}
	catch (Exception^ ex) {
		cli::pin_ptr<const Char> msg = PtrToStringChars(ex->ToString());
		MessageBoxExW(nullptr, msg, L"NppMenuSearch", 0, 0);
		throw;
	}
}

extern "C" __declspec(dllexport) LRESULT __cdecl messageProc(UINT Message, WPARAM wParam, LPARAM lParam)
{
	return 1;
}
