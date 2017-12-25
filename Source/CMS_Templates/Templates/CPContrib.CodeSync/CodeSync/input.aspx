<%@ Page Language="C#" Inherits="CrownPeak.Internal.Debug.InputInit" %>
<%@ Import Namespace="CrownPeak.CMSAPI" %>
<%@ Import Namespace="CrownPeak.CMSAPI.Services" %>
<%@ Import Namespace="CrownPeak.CMSAPI.CustomLibrary" %>
<!--DO NOT MODIFY CODE ABOVE THIS LINE-->
<%//This plugin uses InputContext as its context class type%>
<%

	Input.ShowTextBox("Paths to include", "paths_include", "/System/Library\n/System/Templates", helpMessage:"One directory per line.", height:3);
	Input.ShowTextBox("Paths to exclude", "paths_exclude", "/System/Templates/AdventGeneral\n/System/Templates/SimpleSiteCSharp", helpMessage: "One directory per line", height: 5);

	Input.ShowCheckBox("Package Source Files", "skip_package_sources", "true", "Skip Package Source Files", helpMessage: "Package source files that usually start with an underscore (_).");

	Input.ShowTextBox("Email to", "mail_to");

	Input.ShowTextBox("Modified Since:", "modified_since", helpMessage:"clear this to rebuild all");

	if (string.IsNullOrEmpty(asset["log"]) == false)
	{
		Input.ShowMessage(asset.Raw["log"], MessageType.Basic);
	}
	
%>