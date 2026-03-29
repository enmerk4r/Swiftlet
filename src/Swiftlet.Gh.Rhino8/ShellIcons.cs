using System.Collections.Concurrent;
using System.Drawing;
using System.Reflection;

namespace Swiftlet.Gh.Rhino8;

internal static class ShellIcons
{
    private static readonly ConcurrentDictionary<string, Bitmap?> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string ResourceDirectory = ResolveResourceDirectory();
    private static readonly bool DisableIcons = OperatingSystem.IsLinux();

    public static Bitmap? Logo24() => Cache.GetOrAdd("Logo_24x24.png", LoadBitmap);

    public static Bitmap? Logo16() => Cache.GetOrAdd("Logo_16x16.png", LoadBitmap);

    public static Bitmap? For(Type componentType)
    {
        string fileName = componentType.Name switch
        {
            "ApiKeyAuthComponent" => "Icons_api_auth_24x24.png",
            "Base64ToByteArrayComponent" => "Icons_base64_to_byte_array_24x24.png",
            "Base64ToTextComponent" => "Icons_base64_to_text_24x24.png",
            "BasicAuthComponent" => "Icons_basic_auth_24x24.png",
            "BearerTokenAuthComponent" => "Icons_token_auth_24x24.png",
            "BitmapParam" => "Icons_bitmap_24x24.png",
            "BitmapToByteArrayComponent" => "Icons_bitmap_to_byte_array_24x24.png",
            "BitmapToMeshComponent" => "Icons_byte_array_to_mesh_24x24.png",
            "ByteArrayParam" => "Icons_byte_array_24x24.png",
            "ByteArrayToBase64Component" => "Icons_byte_array_to_base64_24x24.png",
            "ByteArrayToBitmapComponent" => "Icons_byte_array_to_bitmap_24x24.png",
            "ByteArrayToFileComponent" => "Icons_byte_array_to_file_24x24.png",
            "ByteArrayToListComponent" => "Icons_byte_array_to_list_24x24.png",
            "ByteArrayToTextComponent" => "Icons_byte_array_to_text_24x24.png",
            "ColorToHexComponent" => "Icons_color_to_hex_24x24.png",
            "CompressDataComponent" => "Icons_compress_data_24x24.png",
            "CreateByteArrayBodyFromFileComponent" => "Icons_create_byte_array_body_24x24.png",
            "CreateByteArrayBodyComponent" => "Icons_create_byte_array_body_24x24.png",
            "CreateCsvLineComponent" => "Icons_create_csv_line_24x24.png",
            "CreateFormUrlEncodedBodyComponent" => "Icons_create_form_url_encoded_body.png",
            "CreateHttpHeaderComponent" => "Icons_create_http_header_24x24.png",
            "CreateJsonArrayComponent" => "Icons_create_json_array_24x24.png",
            "CreateJsonObjectComponent" => "Icons_create_json_object_24x24.png",
            "CreateJsonValueComponent" => "Icons_create_json_value_24x24.png",
            "CreateMcpEmbeddedBinaryResourceComponent" => "Icons_create_mcp_embedded_binary_resource_24x24.png",
            "CreateMcpEmbeddedTextResourceComponent" => "Icons_create_mcp_embedded_text_resource_24x24.png",
            "CreateMcpImageContentComponent" => "Icons_create_mcp_image_content_24x24.png",
            "CreateMcpResourceLinkComponent" => "Icons_create_mcp_resource_link_24x24.png",
            "CreateMcpTextContentComponent" => "Icons_create_mcp_text_content_24x24.png",
            "CreateMultipartFieldBytesComponent" => "Icons_create_multipart_field_bytes.png",
            "CreateMultipartFieldTextComponent" => "Icons_create_multipart_feild_text.png",
            "CreateMultipartFormBodyComponent" => "Icons_create_multipart_form_body.png",
            "CreateQueryParamComponent" => "Icons_create_query_param_24x24.png",
            "CreateTextBodyComponent" => "Icons_create_text_body_24x24.png",
            "CreateTextBodyCustomComponent" => "Icons_create_text_body_custom_24x24.png",
            "CreateUrlComponent" => "Icons_construct_url_24x24.png",
            "CreateXmlElementComponent" => "Icons_create_xml_element.png",
            "DecompressDataComponent" => "Icons_decompress_data_24x24.png",
            "DeconstructBodyComponent" => "Icons_deconstruct_body.png",
            "DeconstructHeaderComponent" => "Icons_deconstruct_http_header_24x24.png",
            "DeconstructHttpResponseComponent" => "Icons_deconstruct_response_24x24.png",
            "DeconstructMultipartBodyComponent" => "Icons_deconstruct_multipart_body_24x24.png",
            "DeconstructMultipartFieldComponent" => "Icons_deconstruct_multipart_field_24x24.png",
            "DeconstructQueryParamComponent" => "Icons_deconstruct_query_param_24x24.png",
            "DeconstructRequestComponent" => "Icons_deconstruct_request.png",
            "DeconstructToolCallComponent" => "Icons_deconstruct_tool_call.png",
            "DeconstructUrlComponent" => "Icons_deconstruct_url_24x24.png",
            "DefineToolComponent" => "Icons_define_tool.png",
            "DefineToolParameterComponent" => "Icons_define_tool_parameter.png",
            "DeleteRequestComponent" => "Icons_delete_request_24x24.png",
            "DownloadFileComponent" => "Icons_download_file.png",
            "FileToByteArrayComponent" => "Icons_file_to_byte_array_24x24.png",
            "FormatJsonStringComponent" => "Icons_format_json_string_24x24.png",
            "GenerateGuidComponent" => "Icons_generate_guid_24x24.png",
            "GenerateQrCodeComponent" => "Icons_generate_qr_code_24x24.png",
            "GetElementByIdComponent" => "Icons_get_element_by_id_24x24.png",
            "GetElementsByAttributeValueComponent" => "Icons_get_elements_by_attribute_value_24x24.png",
            "GetElementsByClassNameComponent" => "Icons_get_elements_by_class_name_24x24.png",
            "GetElementsByTagNameComponent" => "Icons_get_elements_by_tag_name_24x24.png",
            "GetElementsByXPathComponent" => "Icons_get_elements_by_xpath_24x24.png",
            "GetHtmlAttributeValueComponent" => "Icons_get_attribute_value_24x24.png",
            "GetHtmlAttributesComponent" => "Icons_get_html_attributes_24x24.png",
            "GetHtmlNodeChildrenComponent" => "Icons_get_child_nodes_24x24.png",
            "GetInnerHtmlComponent" => "Icons_get_inner_html_24x24.png",
            "GetJsonObjectKeyComponent" => "Icons_get_json_object_key.png",
            "GetRequestComponent" => "Icons_get_request_24x24.png",
            "GetXmlAttributeValueComponent" => "Icons_get_xml_attribute_value.png",
            "GetXmlAttributesComponent" => "Icons_get_xml_attributes.png",
            "GetXmlChildNodesComponent" => "Icons_get_xml_child_nodes.png",
            "GetXmlElementsByTagComponent" => "Icons_get_xml_elements_by_tag.png",
            "GetXmlElementsByXPathComponent" => "Icons_get_xml_elements_by_xpath.png",
            "GetXmlInnerTextComponent" => "Icons_get_xml_inner_text.png",
            "HexToColorComponent" => "Icons_hex_to_color_24x24.png",
            "HtmlNodeParam" => "Icons_html_node_param_24x24.png",
            "HttpHeaderParam" => "Icons_http_header_param_24x24.png",
            "HttpListenerComponent" => "Icons_http_listener_24x24.png",
            "HttpRequestComponent" => "Icons_http_request_24x24.png",
            "HttpRequestComponentBase" => "Icons_http_request_24x24.png",
            "HttpResponseDataParam" => "Icons_http_response_param_24x24.png",
            "JsonArrayParam" => "Icons_jarray_param_24x24.png",
            "JsonNodeParam" => "Icons_jtoken_param_24x24 .png",
            "JsonObjectParam" => "Icons_jobject_param_24x24.png",
            "JsonValueParam" => "Icons_jvalue_param_24x24.png",
            "ListenerRequestParam" => "Icons_http_request_param.png",
            "ListToByteArrayComponent" => "Icons_list_to_byte_array_24x24.png",
            "McpServerComponent" => "Icons_mcp_server.png",
            "McpContentBlockParam" => "Icons_mcp_tool_response.png",
            "McpToolCallRequestParam" => "Icons_mcp_request_param.png",
            "McpToolDefinitionParam" => "Icons_mcp_tool_definition.png",
            "McpToolParameterParam" => "Icons_mcp_tool_parameter.png",
            "McpToolResponseComponent_ARCHIVED" => "Icons_mcp_tool_response.png",
            "McpToolResponseComponent" => "Icons_mcp_tool_response.png",
            "MergeJsonObjectsComponent" => "Icons_merge_json_objects_24x24.png",
            "MultipartFieldParam" => "Icons_multipart_form_param.png",
            "OAuthAuthorizeComponent" => "Icons_oauth_authorize.png",
            "OAuthTokenComponent" => "Icons_oauth_token.png",
            "ParseJsonStringComponent" => "Icons_parse_json_string_24x24.png",
            "ParseXmlComponent" => "Icons_parse_xml.png",
            "PatchRequestComponent" => "Icons_patch_request_24x24.png",
            "PostRequestComponent" => "Icons_post_request_24x24.png",
            "PutRequestComponent" => "Icons_put_request_24x24.png",
            "QueryParameterParam" => "Icons_query_param_24x24.png",
            "ReadCsvLineComponent" => "Icons_read_csv_line_24x24.png",
            "ReadHtmlComponent" => "Icons_read_html_24x24.png",
            "ReadJsonArrayComponent" => "Icons_read_json_array_24x24.png",
            "ReadJsonObjectComponent" => "Icons_read_json_object_24x24.png",
            "ReadJsonValueComponent" => "Icons_read_json_value_24x24.png",
            "RemoveJsonKeyComponent" => "Icons_remove_json_key_24x24.png",
            "ReplaceEmptyBranchesComponent" => "Icons_replace_empty_branches_24x24.png",
            "RequestBodyParam" => "Icons_request_body_param_24x24.png",
            "SaveCsvComponent" => "Icons_save_csv_24x24.png",
            "SaveTextComponent" => "Icons_save_text_24x24.png",
            "SaveWebResponseComponent" => "Icons_save_web_response_24x24.png",
            "ScreenCaptureActiveViewportComponent" => "Icons_screen_capture_active_viewport_24x24.png",
            "ScreenCaptureViewportComponent" => "Icons_screen_capture_viewport_24x24.png",
            "SeparateJsonTokensComponent" => "Icons_separate_json_tokens_24x24.png",
            "ServerInputComponent" => "Icons_server_input.png",
            "ServerResponseComponent" => "Icons_server_response.png",
            "SetJsonKeyComponent" => "Icons_set_json_key_24x24.png",
            "SplitTextIntoLinesComponent" => "Icons_split_text_into_lines_24x24.png",
            "StringifyHtmlNodeComponent" => "Icons_stringify_html_node_24x24.png",
            "StringifyJsonTokenComponent" => "Icons_stringify_json_token_24x24.png",
            "StringifyXmlNodeComponent" => "Icons_stringify_xml_node.png",
            "TextToBase64Component" => "Icons_text_to_base64_24x24.png",
            "TextToByteArrayComponent" => "Icons_text_to_byte_array_24x24.png",
            "ThrottleComponent" => "Icons_throttle_24x24.png",
            "UdpListenerComponent" => "Icons_udp_listener.png",
            "UdpStreamComponent" => "Icons_udp_stream.png",
            "UploadFileComponent" => "Icons_upload_file.png",
            "UploadFileMultipartComponent" => "Icons_upload_file_multipart.png",
            "UrlEncodeComponent" => "Icons_url_encode_24x24.png",
            "WebSocketClientComponent" => "Icons_socket_listener_24x24.png",
            "WebSocketConnectionParam" => "Icons_websocket_connection_param.png",
            "WebSocketSendComponent" => "Icons_websocket_send.png",
            "WebSocketServerComponent" => "Icons_socket_server.png",
            "XmlNodeParam" => "Icons_xml_node.png",
            _ => "Logo_24x24.png",
        };

        return Cache.GetOrAdd(fileName, LoadBitmap);
    }

    private static Bitmap? LoadBitmap(string fileName)
    {
        if (DisableIcons)
        {
            return null;
        }

        string path = Path.Combine(ResourceDirectory, fileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var stream = new MemoryStream(bytes);
            using var bitmap = new Bitmap(stream);
            return new Bitmap(bitmap);
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveResourceDirectory()
    {
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
        return Path.Combine(assemblyDirectory, "Resources");
    }
}
