using CommunityToolkit.Maui.Views;

namespace BasculaInterface.Views.PopUps;

public partial class WaitPopUp : Popup
{
	public WaitPopUp()
	{
		InitializeComponent();
	}
	public WaitPopUp(string message) : this()
	{
		LblMessage.Text = message;
    }
}