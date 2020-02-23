namespace Scarlet.Api.Game
{
	// marker interfaces are generally considered a code smell, but see here:
	// https://autofaccn.readthedocs.io/en/latest/faq/select-by-context.html#option-1-redesign-your-interfaces

	public interface IEEScarletGameApi : IScarletGameApi
	{
	}

	public interface IEEUScarletGameApi : IScarletGameApi
	{
	}
}