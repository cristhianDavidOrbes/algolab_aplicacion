import { proxyBackend } from "@/lib/backend";

type RouteContext = {
  params: Promise<{
    id: string;
  }>;
};

export async function GET(request: Request, context: RouteContext) {
  const { id } = await context.params;

  return proxyBackend({
    request,
    path: `/api/usuarios/${id}`,
    method: "GET",
  });
}

export async function PUT(request: Request, context: RouteContext) {
  const { id } = await context.params;

  return proxyBackend({
    request,
    path: `/api/usuarios/${id}`,
    method: "PUT",
  });
}

export async function PATCH(request: Request, context: RouteContext) {
  const { id } = await context.params;

  return proxyBackend({
    request,
    path: `/api/usuarios/${id}`,
    method: "PATCH",
  });
}

export async function DELETE(request: Request, context: RouteContext) {
  const { id } = await context.params;

  return proxyBackend({
    request,
    path: `/api/usuarios/${id}`,
    method: "DELETE",
  });
}
