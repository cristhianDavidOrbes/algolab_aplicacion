import { proxyBackend } from "@/lib/backend";

export async function GET(request: Request) {
  return proxyBackend({
    request,
    path: "/api/usuarios",
    method: "GET",
  });
}

export async function POST(request: Request) {
  return proxyBackend({
    request,
    path: "/api/usuarios",
    method: "POST",
  });
}
