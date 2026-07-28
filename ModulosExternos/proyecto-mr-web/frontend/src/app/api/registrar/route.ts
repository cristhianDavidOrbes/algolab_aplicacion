import { proxyBackend } from "@/lib/backend";

export async function POST(request: Request) {
  return proxyBackend({
    request,
    path: "/api/usuarios/registrar",
    method: "POST",
  });
}
