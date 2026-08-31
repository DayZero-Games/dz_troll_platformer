using System;

namespace DZ.Core.Contracts
{
	[Serializable]
	public enum AudioId : short
	{
		None,
		BackgroundMusic,
		Walk,
		Jump,
		Death,
		ExitDoorReached,
		UIButtonPressed
	}
}