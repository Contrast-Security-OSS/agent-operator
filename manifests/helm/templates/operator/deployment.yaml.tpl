{{ if ne .Values.operator.enabled false }}
apiVersion: apps/v1
kind: Deployment
metadata:
  name: contrast-agent-operator
  namespace: '{{ default .Release.Namespace .Values.namespace }}'
  labels:
    app.kubernetes.io/name: operator
    app.kubernetes.io/part-of: contrast-agent-operator
spec:
  replicas: 1
  revisionHistoryLimit: 1
  selector:
    matchLabels:
      app.kubernetes.io/name: operator
      app.kubernetes.io/part-of: contrast-agent-operator
  template:
    metadata:
      labels:
        app.kubernetes.io/name: operator
        app.kubernetes.io/part-of: contrast-agent-operator
    spec:
      affinity:
        nodeAffinity:
          requiredDuringSchedulingIgnoredDuringExecution:
            nodeSelectorTerms:
              - matchExpressions:
                  - key: kubernetes.io/os
                    operator: In
                    values:
                      - linux
                  - key: kubernetes.io/arch
                    operator: In
                    values:
                      - amd64
                      - arm64
        podAntiAffinity:
          preferredDuringSchedulingIgnoredDuringExecution:
            - weight: 100
              podAffinityTerm:
                labelSelector:
                  matchExpressions:
                    - key: app.kubernetes.io/name
                      operator: In
                      values:
                        - operator
                    - key: app.kubernetes.io/part-of
                      operator: In
                      values:
                        - contrast-agent-operator
                topologyKey: kubernetes.io/hostname
      serviceAccountName: contrast-agent-operator-service-account
      imagePullSecrets:
        - name: '{{ .Values.imageCredentials.pullSecretName }}'
      containers:
        - name: contrast-agent-operator
          image: '{{ .Values.image.registry }}/{{ .Values.image.repository }}:{{ default .Chart.AppVersion .Values.image.tag }}'
          imagePullPolicy: Always
          securityContext:
            allowPrivilegeEscalation: false
            readOnlyRootFilesystem: true
            runAsNonRoot: true
            capabilities:
              drop:
              - ALL
          ports:
            - containerPort: 5001
              name: https
          env:
            - name: POD_NAMESPACE
              valueFrom:
                fieldRef:
                  fieldPath: metadata.namespace
            - name: CONTRAST_WEBHOOK_SERVICENAME
              value: contrast-agent-operator
            - name: CONTRAST_WEBHOOK_HOSTS
              value: $(CONTRAST_WEBHOOK_SERVICENAME),$(CONTRAST_WEBHOOK_SERVICENAME).$(POD_NAMESPACE).svc,$(CONTRAST_WEBHOOK_SERVICENAME).$(POD_NAMESPACE).svc.cluster.local
            - name: CONTRAST_DEFAULT_REGISTRY
              value: '{{ required "operator.defaultRegistry is required." .Values.operator.defaultRegistry }}'
            - name: CONTRAST_SETTLE_DURATION
              value: '{{ .Values.operator.settleDuration }}'
            - name: CONTRAST_EVENT_QUEUE_SIZE
              value: '{{ .Values.operator.eventQueueSize }}'
            - name: CONTRAST_EVENT_QUEUE_FULL_MODE
              value: '{{ .Values.operator.eventQueueFullMode }}'
            - name: CONTRAST_WEBHOOK_SECRET
              value: '{{ .Values.operator.webhookSecretName }}'
            - name: CONTRAST_WEBHOOK_CONFIGURATION
              value: '{{ .Values.operator.webhookConfiguration }}'
            - name: CONTRAST_ENABLE_EARLY_CHAINING
              value: '{{ .Values.operator.enableEarlyChaining }}'
            - name: CONTRAST_INSTALL_SOURCE
              value: helm
            - name: CONTRAST_INITCONTAINER_CPU_REQUEST
              value: '{{ .Values.operator.initContainer.resources.requests.cpu }}'
            - name: CONTRAST_INITCONTAINER_CPU_LIMIT
              value: '{{ .Values.operator.initContainer.resources.limits.cpu }}'
            - name: CONTRAST_INITCONTAINER_MEMORY_REQUEST
              value: '{{ .Values.operator.initContainer.resources.requests.memory }}'
            - name: CONTRAST_INITCONTAINER_MEMORY_LIMIT
              value: '{{ .Values.operator.initContainer.resources.limits.memory }}'
            - name: CONTRAST_INITCONTAINER_EPHEMERALSTORAGE_REQUEST
              value: '{{ .Values.operator.initContainer.resources.requests.ephemeralStorage }}'
            - name: CONTRAST_INITCONTAINER_EPHEMERALSTORAGE_LIMIT
              value: '{{ .Values.operator.initContainer.resources.limits.ephemeralStorage }}'
          livenessProbe:
            httpGet:
              path: /health
              port: 5001
              scheme: HTTPS
          readinessProbe:
            httpGet:
              path: /ready
              port: 5001
              scheme: HTTPS
          resources: # Very liberal here, likely not needed.
            limits:
              cpu: 2000m
              memory: 512Mi
            requests:
              cpu: 500m
              memory: 256Mi
          volumeMounts: # We set readOnlyRootFilesystem but ASP.NET Core will buffer requests to disk under high load
            - name: tmpfs
              mountPath: /tmp
      volumes:
        - name: tmpfs
          emptyDir:
            sizeLimit: 50Mi
{{ end }}
