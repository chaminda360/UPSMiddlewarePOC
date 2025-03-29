<% 
' --- Send Request to Middleware ---
Dim middlewareUrl, apiKey, fromZip, toZip, weight
middlewareUrl = "https://your-middleware.com/api/ups/rates"
apiKey = "YOUR_SECRET_API_KEY" ' Must match appsettings.json
fromZip = "10001"
toZip = "90001"
weight = 5

' Build JSON request
Dim jsonRequest
jsonRequest = "{""fromZip"":""" & fromZip & """,""toZip"":""" & toZip & """,""weight"":" & weight & "}"

' Create HTTP request
Dim httpRequest, responseText
Set httpRequest = Server.CreateObject("MSXML2.ServerXMLHTTP.6.0")
httpRequest.Open "POST", middlewareUrl, False
httpRequest.setRequestHeader "Content-Type", "application/json"
httpRequest.setRequestHeader "X-API-Key", apiKey
httpRequest.Send jsonRequest

' --- Handle Response ---
If httpRequest.Status = 200 Then
    responseText = httpRequest.responseText
    
    ' Simple JSON parsing (for Classic ASP)
    Dim groundRate, airRate
    groundRate = ExtractJSONValue(responseText, "GroundRate"":""")
    airRate = ExtractJSONValue(responseText, "AirRate"":""")
    
    Response.Write "UPS Ground: $" & groundRate & "<br>"
    Response.Write "UPS 2-Day Air: $" & airRate
Else
    Response.Write "Error " & httpRequest.Status & ": " & httpRequest.statusText
End If

' Helper function to extract JSON values
Function ExtractJSONValue(json, key)
    Dim startPos, endPos
    startPos = InStr(json, key) + Len(key)
    endPos = InStr(startPos, json, """")
    If startPos > Len(key) And endPos > startPos Then
        ExtractJSONValue = Mid(json, startPos, endPos - startPos)
    Else
        ExtractJSONValue = "N/A"
    End If
End Function
%>