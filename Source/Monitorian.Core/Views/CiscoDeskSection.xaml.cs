using System.Windows.Controls;

using Monitorian.Core.ViewModels;
using Monitorian.Core.Views.Controls;

namespace Monitorian.Core.Views;

public partial class CiscoDeskSection : UserControl
{
	private readonly CiscoDeskSectionViewModel _viewModel;

	public CiscoDeskSection(AppControllerCore controller)
	{
		InitializeComponent();

		this.DataContext = _viewModel = new CiscoDeskSectionViewModel(controller);

		// PasswordBox does not support binding.
		PasswordBox.Password = _viewModel.Password ?? string.Empty;
		PasswordBox.PasswordChanged += (_, _) => _viewModel.Password = PasswordBox.Password;

		FlowElement.EnsureFlowDirection(this);
	}
}
