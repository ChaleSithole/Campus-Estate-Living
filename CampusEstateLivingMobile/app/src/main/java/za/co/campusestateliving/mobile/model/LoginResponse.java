package za.co.campusestateliving.mobile.model;

import org.json.JSONArray;
import org.json.JSONObject;
import java.util.ArrayList;
import java.util.List;

public final class LoginResponse {
    public final String token, expiresAt, userNumber, email, fullName;
    public final int userId;
    public final List<String> roles;
    private LoginResponse(String token, String expiresAt, int userId, String userNumber, String email, String fullName, List<String> roles) {
        this.token=token; this.expiresAt=expiresAt; this.userId=userId; this.userNumber=userNumber; this.email=email; this.fullName=fullName; this.roles=roles;
    }
    public static LoginResponse fromJson(JSONObject json) throws Exception {
        List<String> roles = new ArrayList<>();
        JSONArray array = json.optJSONArray("roles");
        if (array != null) for (int i=0; i<array.length(); i++) roles.add(array.getString(i));
        return new LoginResponse(json.getString("token"), json.optString("expiresAt"), json.optInt("userId"), json.optString("userNumber"), json.optString("email"), json.optString("fullName"), roles);
    }
    public String primaryRole() { return roles.isEmpty() ? "User" : roles.get(0); }
}
