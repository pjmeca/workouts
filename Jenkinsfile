#!groovy

pipeline {
    agent any
    environment {
        IMAGE_NAME = 'pjmeca/workouts'
        DOCKER_COMPOSE_FILE = '/opt/docker/compose-files/workouts.yml'
        TMP_DIR = '/tmp/jenkins/workouts/production'
    }
    stages {
        stage('Docker Build') {
            steps {
                sh 'docker build -t ${IMAGE_NAME}:production .'
                sh 'docker image tag ${IMAGE_NAME}:production ${IMAGE_NAME}:latest'
            }
        }
        stage('Docker Deploy') {
            steps {
                sh 'mkdir -p ${TMP_DIR}'
                dir(env.TMP_DIR) {
                    sh 'cp ${DOCKER_COMPOSE_FILE} ./docker-compose.yml'
                    sh 'docker compose up -d'
                }
            }
        }
    }
}
