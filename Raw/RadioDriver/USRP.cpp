#include <stdio.h>
#include <stdlib.h>
#include <uhd/usrp/multi_usrp.hpp>

using namespace uhd;
using namespace uhd::usrp;

multi_usrp::sptr s_Radio = NULL;
rx_streamer::sptr s_RX = NULL;
tx_streamer::sptr s_TX = NULL;
uhd::tx_metadata_t s_TXMD;

extern "C" __declspec(dllexport) int Init()
{
	if( s_Radio == NULL ) {
		try {
			device_addr_t params("");
			s_Radio = multi_usrp::make(params);
			s_Radio->set_clock_source("internal");
			return 0;
		} catch( uhd::key_error a ) {
			return -1;
		} catch( uhd::assertion_error a ) {
			return -1;
		}
	}
	return 0;
}

extern "C" __declspec(dllexport) int InitRX()
{
	if( s_RX == NULL ) {
		try {
			stream_args_t stream_args("fc64", "sc16");
			s_RX = s_Radio->get_rx_stream(stream_args);
			return 0;
		} catch( uhd::key_error a ) {
			return -1;
		} catch( uhd::assertion_error a ) {
			return -1;
		}
	}
	return 0;
}

extern "C" __declspec(dllexport) int InitTX()
{
	if( s_TX == NULL ) {
		try {
			stream_args_t stream_args("fc64", "sc16");
			s_TX = s_Radio->get_tx_stream(stream_args);
			s_TXMD.start_of_burst = true;
			s_TXMD.end_of_burst   = false;
			s_TXMD.has_time_spec  = false;
			s_TXMD.time_spec = uhd::time_spec_t(0.01);
			s_Radio->set_time_now(uhd::time_spec_t(0.0));
			s_TX->send((double*)NULL, 0, s_TXMD, 1.0);
			return 0;
		} catch( uhd::key_error a ) {
			return -1;
		} catch( uhd::assertion_error a ) {
			return -1;
		}
	}
	return 0;
}

extern "C" __declspec(dllexport) int SetRXFrequency(double frequency)
{
	try {
		tune_request_t tuneRequest = tune_request_t(frequency);
		s_Radio->set_rx_freq(tuneRequest);
		return 0;
	} catch( uhd::key_error a ) {
		return -1;
	} catch( uhd::assertion_error a ) {
		return -1;
	}
}

extern "C" __declspec(dllexport) int SetTXFrequency(double frequency)
{
	try {
		tune_request_t tuneRequest = tune_request_t(frequency);
		s_Radio->set_tx_freq(tuneRequest);
		return 0;
	} catch( uhd::key_error a ) {
		return -1;
	} catch( uhd::assertion_error a ) {
		return -1;
	}
}

extern "C" __declspec(dllexport) int SetRXSampleRate(double rate)
{
	try {
		s_Radio->set_rx_rate(rate);
		return 0;
	} catch( uhd::key_error a ) {
		return -1;
	} catch( uhd::assertion_error a ) {
		return -1;
	}
}

extern "C" __declspec(dllexport) int SetTXSampleRate(double rate)
{
	try {
		s_Radio->set_tx_rate(rate);
		return 0;
	} catch( uhd::key_error a ) {
		return -1;
	} catch( uhd::assertion_error a ) {
		return -1;
	}
}

extern "C" __declspec(dllexport) int SetRXGain(double gain)
{
	try {
		s_Radio->set_rx_gain(gain);
		return 0;
	} catch( uhd::key_error a ) {
		return -1;
	} catch( uhd::assertion_error a ) {
		return -1;
	}
}

extern "C" __declspec(dllexport) int SetTXGain(double gain)
{
	try {
		s_Radio->set_tx_gain(gain);
		return 0;
	} catch( uhd::key_error a ) {
		return -1;
	} catch( uhd::assertion_error a ) {
		return -1;
	}
}

extern "C" __declspec(dllexport) int StartReceiving()
{
	try {
		stream_cmd_t stream_cmd(stream_cmd_t::STREAM_MODE_START_CONTINUOUS);
		stream_cmd.stream_now = true;
		stream_cmd.time_spec = uhd::time_spec_t();
		s_Radio->issue_stream_cmd(stream_cmd);
		return 0;
	} catch( uhd::key_error a ) {
		return -1;
	} catch( uhd::assertion_error a ) {
		return -1;
	}
}

struct df
{
	double r;
	double i;
};

extern "C" __declspec(dllexport) int ReceiveSamples(double* samples, int maxSamples)
{
	uhd::rx_metadata_t md;
	int numSamples = s_RX->recv((df*)samples, maxSamples, md, 1.0);
	if (md.error_code == uhd::rx_metadata_t::ERROR_CODE_NONE)
	{
		return numSamples;
	}
	return 0;
}

;

extern "C" __declspec(dllexport) int SendSamples(double* samples, int numSamples)
{
	int sentSamples = s_TX->send((df*)samples, numSamples, s_TXMD, 1.0);
	s_TXMD.start_of_burst = false;
	s_TXMD.time_spec += uhd::time_spec_t(0, sentSamples, s_Radio->get_tx_rate());
	return sentSamples;
}
