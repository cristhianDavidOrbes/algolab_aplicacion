package com.algolab.backend_werb_mr;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

import com.algolab.backend_werb_mr.configuracion.RailwayDatabaseUrlInitializer;

@SpringBootApplication
public class BackendWerbMrApplication {

	public static void main(String[] args) {
		SpringApplication application = new SpringApplication(BackendWerbMrApplication.class);
		application.addInitializers(new RailwayDatabaseUrlInitializer());
		application.run(args);
	}

}
