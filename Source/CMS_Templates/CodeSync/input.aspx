﻿<%@ Page Language="C#" Inherits="CrownPeak.Internal.Debug.InputInit" %>
<%@ Import Namespace="CrownPeak.CMSAPI" %>
<%@ Import Namespace="CrownPeak.CMSAPI.Services" %>
<%@ Import Namespace="CrownPeak.CMSAPI.CustomLibrary" %>
<!--DO NOT MODIFY CODE ABOVE THIS LINE-->
<%//This plugin uses InputContext as its context class type%>
<%
// input.aspx: template file to specify content entry input form

	Input.ShowTextBox("Email to", "mail_to");

%>