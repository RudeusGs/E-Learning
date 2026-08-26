FROM node:22-alpine AS build
WORKDIR /app
ENV HUSKY=0

COPY package.json package-lock.json ./
COPY client/package.json ./client/package.json
RUN npm ci --no-audit --no-fund --ignore-scripts

COPY client ./client
RUN npm run build:client

FROM nginx:1.29-alpine AS runtime
COPY client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/client/dist /usr/share/nginx/html
EXPOSE 80
