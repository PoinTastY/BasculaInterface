﻿using Android.App;
using Android.Runtime;

namespace BasculaInterface.Platforms.Android;

[Application(UsesCleartextTraffic = true)]
public class MainApplication : MauiApplication
{
	public MainApplication(nint handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
