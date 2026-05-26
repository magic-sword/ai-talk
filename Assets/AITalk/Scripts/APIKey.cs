using System;
using System.Collections.Generic;

/// <summary>
/// API KeyをJSONデータと変換しやすくするために定義するクラス
/// </summary>
[Serializable]
public class APIKey
{
    public Installed installed;
    
    [Serializable]
    public class Installed
    {
        public string client_id;
        public string project_id;
        public string auth_uri;
        public string token_uri;
        public string auth_provider_x509_cert_url;
        public string client_secret;
        public List<string> redirect_uris;
    }
}
