{{/* vim: set filetype=mustache: */}}

{{/*
Expand the name of the chart.
*/}}
{{- define "durabletask-azurestorage-scaler.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains the word "scaler" (or if the overrien name) it will be used as a full name.
*/}}
{{- define "durabletask-azurestorage-scaler.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default "scaler" .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create the name of the service account to use.
*/}}
{{- define "durabletask-azurestorage-scaler.serviceAccountName" -}}
{{- default (include "durabletask-azurestorage-scaler.fullname" .) .Values.serviceAccount.name }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "durabletask-azurestorage-scaler.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels.
*/}}
{{- define "durabletask-azurestorage-scaler.labels" }}
helm.sh/chart: {{ include "durabletask-azurestorage-scaler.chart" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/part-of: {{ .Chart.Name }}
app.kubernetes.io/version: {{ .Chart.Version | quote }}
{{- if .Values.additionalLabels }}
{{ toYaml .Values.additionalLabels }}
{{- end }}
{{- end }}

{{/*
Derive the port used for readiness checks.
*/}}
{{- define "durabletask-azurestorage-scaler.readinessPort" -}}
{{ add .Values.port 1 }}
{{- end }}