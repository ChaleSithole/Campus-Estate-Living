package za.co.campusestateliving.mobile.api;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;

/** HTTP client for the Campus Estate Living API. Base URL comes from ApiBaseUrl. No request bodies are logged. */
public final class ApiClient {
    private ApiClient() {}

    public static JSONObject post(String relativePath, JSONObject body) throws Exception {
        return asObject(exchange("POST", relativePath, null, body == null ? null : body.toString()));
    }

    public static JSONObject post(String relativePath, JSONObject body, String token) throws Exception {
        return asObject(exchange("POST", relativePath, token, body == null ? "{}" : body.toString()));
    }

    public static JSONObject put(String relativePath, JSONObject body, String token) throws Exception {
        return asObject(exchange("PUT", relativePath, token, body == null ? "{}" : body.toString()));
    }

    public static JSONObject delete(String relativePath, String token) throws Exception {
        return asObject(exchange("DELETE", relativePath, token, null));
    }

    public static JSONObject get(String relativePath, String token) throws Exception {
        return getObject(relativePath, token);
    }

    public static JSONObject getObject(String relativePath, String token) throws Exception {
        return asObject(exchange("GET", relativePath, token, null));
    }

    public static JSONArray getArray(String relativePath, String token) throws Exception {
        String raw = exchange("GET", relativePath, token, null);
        if (raw == null || raw.trim().isEmpty()) return new JSONArray();
        return new JSONArray(raw);
    }

    private static String exchange(String method, String relativePath, String token, String jsonBody) throws Exception {
        HttpURLConnection connection = (HttpURLConnection) new URL(ApiBaseUrl.current() + relativePath).openConnection();
        try {
            connection.setRequestMethod(method);
            connection.setConnectTimeout(15000);
            connection.setReadTimeout(20000);
            connection.setRequestProperty("Accept", "application/json");
            if (token != null && !token.isEmpty()) {
                connection.setRequestProperty("Authorization", "Bearer " + token);
            }
            if (jsonBody != null) {
                connection.setRequestProperty("Content-Type", "application/json; charset=utf-8");
                connection.setDoOutput(true);
                byte[] payload = jsonBody.getBytes(StandardCharsets.UTF_8);
                try (OutputStream out = connection.getOutputStream()) {
                    out.write(payload);
                }
            }
            int status = connection.getResponseCode();
            InputStream stream = status >= 200 && status < 300
                    ? connection.getInputStream()
                    : connection.getErrorStream();
            StringBuilder response = new StringBuilder();
            if (stream != null) {
                try (BufferedReader reader = new BufferedReader(new InputStreamReader(stream, StandardCharsets.UTF_8))) {
                    String line;
                    while ((line = reader.readLine()) != null) response.append(line);
                }
            }
            if (status < 200 || status >= 300) {
                throw new ApiException(messageFrom(response.toString(), status), status);
            }
            return response.toString();
        } finally {
            connection.disconnect();
        }
    }

    private static JSONObject asObject(String raw) throws Exception {
        if (raw == null || raw.trim().isEmpty()) return new JSONObject();
        return new JSONObject(raw);
    }

    private static String messageFrom(String raw, int status) {
        String fallback = status == 403
                ? "You do not have permission for that action."
                : "Request failed (HTTP " + status + ")";
        if (raw == null || raw.isEmpty()) return fallback;
        try {
            JSONObject error = new JSONObject(raw);
            if (error.has("message") && !error.optString("message").isEmpty()) {
                return error.optString("message");
            }
            if (error.has("errors")) {
                Object errors = error.get("errors");
                if (errors instanceof JSONArray && ((JSONArray) errors).length() > 0) {
                    return ((JSONArray) errors).optString(0, fallback);
                }
            }
            if (error.has("title") && !error.optString("title").isEmpty()) {
                return error.optString("title");
            }
        } catch (Exception ignored) {
        }
        return fallback;
    }

    public static final class ApiException extends Exception {
        public final int statusCode;

        public ApiException(String message, int statusCode) {
            super(message);
            this.statusCode = statusCode;
        }
    }
}
