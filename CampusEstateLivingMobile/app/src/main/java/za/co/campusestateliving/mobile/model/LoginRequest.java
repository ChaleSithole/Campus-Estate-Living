package za.co.campusestateliving.mobile.model;

import org.json.JSONObject;

public final class LoginRequest {
    public final String email;
    public final String password;
    public LoginRequest(String email, String password) { this.email = email; this.password = password; }
    public JSONObject toJson() throws Exception {
        JSONObject json = new JSONObject();
        json.put("email", email);
        json.put("password", password);
        return json;
    }
}
