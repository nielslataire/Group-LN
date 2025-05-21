Imports System.Globalization
Imports System.Web.Mvc

Public Class DecimalModelBinder
    Implements IModelBinder
    Public Function BindModel(controllerContext As ControllerContext, bindingContext As ModelBindingContext) As Object Implements IModelBinder.BindModel
        '''''''''Dim valueResult As ValueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName)
        '''''''''Dim modelState As New ModelState() With {
        '''''''''    .Value = valueResult
        '''''''''}
        '''''''''Dim actualValue As Object = Nothing
        '''''''''Try
        '''''''''    actualValue = Convert.ToDecimal(valueResult.AttemptedValue, CultureInfo.CurrentCulture)
        '''''''''Catch e As FormatException
        '''''''''    modelState.Errors.Add(e)
        '''''''''End Try

        '''''''''bindingContext.ModelState.Add(bindingContext.ModelName, modelState)
        '''''''''Return actualValue

        Dim result As Object = Nothing

        ' Don't do this here!
        ' It might do bindingContext.ModelState.AddModelError
        ' and there is no RemoveModelError!
        ' 
        ' result = base.BindModel(controllerContext, bindingContext);

        Dim modelName As String = bindingContext.ModelName
        Dim attemptedValue As String = bindingContext.ValueProvider.GetValue(modelName).AttemptedValue

        ' Depending on CultureInfo, the NumberDecimalSeparator can be "," or "."
        ' Both "." and "," should be accepted, but aren't.
        Dim wantedSeperator As String = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator
        Dim alternateSeperator As String = (If(wantedSeperator = ",", ".", ","))

        If attemptedValue.IndexOf(wantedSeperator) = -1 AndAlso attemptedValue.IndexOf(alternateSeperator) <> -1 Then
            attemptedValue = attemptedValue.Replace(alternateSeperator, wantedSeperator)
        End If

        Try
            If bindingContext.ModelMetadata.IsNullableValueType AndAlso String.IsNullOrWhiteSpace(attemptedValue) Then
                Return Nothing
            End If

            result = Decimal.Parse(attemptedValue, NumberStyles.Any)
        Catch e As FormatException
            bindingContext.ModelState.AddModelError(modelName, e)
        End Try

        Return result
    End Function
End Class
